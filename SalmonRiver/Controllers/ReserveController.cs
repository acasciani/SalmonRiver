using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SalmonRiver;
using SalmonRiver.Models;
using RestSharp;
using System.Configuration;
using System.Web.Script.Serialization;
using Postal;

namespace SalmonRiver.Controllers
{
    public class ReserveController : Controller
    {
        public const int HoldLength = 15; // minutes
        private static string _SquareEndpoint = null;
        private static string _SquareAccessToken = null;

        public string SquareEndpoint { get { return _SquareEndpoint; } }
        public string SquareAccessToken { get { return _SquareAccessToken; } }

        private SalmonRiverEntities db = new SalmonRiverEntities();

        public ReserveController()
        {
            if (_SquareEndpoint == null)
            {
                _SquareEndpoint = ConfigurationManager.AppSettings["SquareEndpoint"];
            }

            if (_SquareAccessToken == null)
            {
                _SquareAccessToken = ConfigurationManager.AppSettings["SquareAccessToken"];
            }
        }

        // GET: Reserve
        public ActionResult Index()
        {
            TemporaryReservationViewModel tempReservation = Session["Hold"] as TemporaryReservationViewModel;

            if (tempReservation == null || tempReservation.Expires <= DateTime.Now.ToUniversalTime())
            {
                return View();
            }
            else
            {
                return View(tempReservation);
            }
        }

        [HttpGet]
        public ActionResult Completed(int? id)
        {
            TransactionLog log = id.HasValue ? db.TransactionLogs.Where(i => i.TransactionID == id.Value).FirstOrDefault() : null;

            if (log == null)
            {
                return RedirectToAction("Index");
            }

            return View(log);
        }

        [HttpGet]
        public ActionResult Failed(int? id)
        {
            TransactionLog log = id.HasValue ? db.TransactionLogs.Where(i => i.TransactionID == id.Value).FirstOrDefault() : null;

            if (log == null)
            {
                return RedirectToAction("Index");
            }

            return View(log);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Index([Bind(Include = "Holds,GuestCount,FullName,EmailAddress,PhoneNumber,CardNonce")] TemporaryReservationViewModel tempReservation)
        {
            if (ModelState.IsValid)
            {
                // Make sure we have the valid charge amount and no one messed around with post variables.
                tempReservation.UpdateTotalCost();

                Guid idempotencyKey = Guid.NewGuid();

                TransactionLog log = null;

                try
                {
                    // everything is valid, charge the person
                    var locationID = ObtainSquareLocationID();

                    SquareErrors charged = ChargeNonce(locationID, tempReservation.CardNonce, tempReservation.TotalCost, idempotencyKey);

                    if (charged.errors != null && charged.errors.Count() > 0)
                    {
                        // check if nonce was already used (like a page refresh)
                        if (charged.errors.Count(i => i.code == "CARD_TOKEN_USED") > 0)
                        {
                            return RedirectToAction("Index");
                        }

                        ViewBag.SquareErrors = charged;
                        return View(tempReservation);
                    }
                    else
                    {
                        // add to transaction log and convert temp reservation to permenant reservation... then email
                        log = new TransactionLog()
                        {
                            CardNonce = tempReservation.CardNonce,
                            ErrorMessage = null,
                            IdempotencyKey = idempotencyKey,
                            ReferenceKey = Guid.NewGuid(),
                            Successful = true,
                            Timestamp = DateTime.UtcNow
                        };

                        Reservation reservation = new Reservation()
                        {
                            AmountPaid = tempReservation.TotalCost,
                            CreateDate = DateTime.UtcNow,
                            EmailAddress = tempReservation.EmailAddress,
                            FullName = tempReservation.FullName,
                            PhoneNumber = tempReservation.PhoneNumber,
                            GuestCount = tempReservation.GuestCount
                        };

                        List<int> dateIDs = tempReservation.Holds.Select(i => i.DateID).Distinct().ToList();
                        var dates = db.Dates.Where(i => dateIDs.Contains(i.DateID)).ToList();
                        dates.ForEach(i => reservation.ReservationDates.Add(new ReservationDate()
                        {
                            DateID = i.DateID
                        }));

                        log.Reservations.Add(reservation);
                        db.TransactionLogs.Add(log);
                        db.SaveChanges();

                        // send email
                        dynamic email = new Email("ReservationSuccessful");
                        email.To = reservation.EmailAddress;
                        email.FullName = reservation.FullName;
                        email.CheckIn = reservation.ReservationDates.OrderBy(i => i.Date.Date1).First().Date.CheckIn;
                        email.CheckOut = reservation.ReservationDates.OrderByDescending(i => i.Date.Date1).First().Date.CheckOut;
                        email.GuestCount = reservation.GuestCount;
                        email.TotalCost = reservation.AmountPaid.ToString("C");
                        email.ReferenceNumber = reservation.TransactionLog.ReferenceKey;
                        email.Send();

                        Session.Remove("Hold");

                        return RedirectToAction("Completed", new { id = log.TransactionID });
                    }
                }
                catch (Exception ex)
                {
                    log = new TransactionLog()
                    {
                        CardNonce = tempReservation.CardNonce,
                        ErrorMessage = ex.Message,
                        IdempotencyKey = idempotencyKey,
                        ReferenceKey = Guid.NewGuid(),
                        Successful = false,
                        Timestamp = DateTime.UtcNow
                    };

                    tempReservation.ExtendExpiration(48*60); // extend by 2 days

                    db.TransactionLogs.Add(log);
                    db.SaveChanges();

                    Session.Remove("Hold");

                    return RedirectToAction("Failed", new { id = log.TransactionID });
                }
            }

            return View(tempReservation);
        }


        #region square methods
        private SquareErrors ChargeNonce(string locationID, string cardNonce, decimal amount, Guid idempotencyKey)
        {
            RestClient client = new RestClient(SquareEndpoint);

            var request = new RestRequest("locations/{location}/transactions", Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddUrlSegment("location", locationID);

            request.AddHeader("Authorization", "Bearer " + SquareAccessToken);
            request.AddHeader("Accept", "application/json");

            // the amount is in cents... see: http://stackoverflow.com/questions/37376062/submitting-a-charge-to-square-with-a-decimal-amount/
            int amountInCents = Convert.ToInt32(decimal.Multiply(amount, 100M));

            string json = "{"+
                "\"card_nonce\": \""+cardNonce+"\"," +
                "\"amount_money\": {" +
                "\"amount\": " + amountInCents + "," +
                "\"currency\": \"USD\"" + 
                "},"+
                "\"idempotency_key\": \"" + idempotencyKey.ToString() + "\"" +
                "  }";

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            return client.Execute<SquareErrors>(request).Data;
        }

        private string ObtainSquareLocationID()
        {
            RestClient client = new RestClient(SquareEndpoint);

            var request = new RestRequest("locations", Method.GET);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", "Bearer " + SquareAccessToken);

            SquareLocations response = client.Execute<SquareLocations>(request).Data;
            return response.locations[0].id;
        }
        #endregion


        [HttpPost]
        public ActionResult CancelTemporaryHold()
        {
            TemporaryReservationViewModel tempHold = Session["Hold"] as TemporaryReservationViewModel;
            List<int> holdsToCancel = tempHold.Holds.Select(i => i.HoldID).Distinct().ToList();

            List<Hold> backDate = db.Holds.Where(i => holdsToCancel.Contains(i.HoldID)).ToList();
            backDate.ForEach(i => i.Expiration = DateTime.Now.ToUniversalTime());
            db.SaveChanges();
            Session.Remove("Hold");

            return Json(true);
        }

        [NonAction]
        public Errors? ValidateBookNowModel(BookNowViewModel model)
        {
            if (model.Start < DateTime.Today || model.End < DateTime.Today || model.End < model.Start)
            {
                return Errors.BookNow_InvalidStartOrEndDate;
            }

            if (model.Guests < 1 || model.Guests > 6)
            {
                return Errors.BookNow_InvalidGuestSelection;
            }

            return null;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
