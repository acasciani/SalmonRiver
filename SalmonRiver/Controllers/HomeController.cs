using SalmonRiver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SalmonRiver.Controllers
{
    public class HomeController : Controller
    {
        private SalmonRiverEntities db = new SalmonRiverEntities();

        private List<DateTime> GetAvailableDates()
        {
            DateTime expiration = DateTime.Now.ToUniversalTime();
            DateTime maxBookDate = DateTime.Today.AddMonths(6);
            List<DateTime> onHold = db.Holds.Where(i => expiration < i.Expiration).Select(i => i.Date.Date1).Distinct().ToList();
            List<DateTime> currentlyReserved = db.ReservationDates.Select(i => i.Date.Date1).Distinct().ToList();
            return db.Dates.Where(i => i.IsActive && i.Date1 >= DateTime.Today && i.Date1 <= maxBookDate && !onHold.Contains(i.Date1) && !currentlyReserved.Contains(i.Date1)).Select(i => i.Date1).Distinct().ToList();
        }

        public ActionResult Index(int? error)
        {
            ViewBag.AvailableDates = GetAvailableDates();
            ViewBag.ErrorCode = error.HasValue ? (Errors)error.Value : (Errors?)null;

            return View();
        }

        [HttpPost]
        public ActionResult Index(BookNowViewModel bookNow)
        { 
            // now check to see that the model is actually passes validation
            using (ReserveController rc = new ReserveController())
            {
                Errors? errors = rc.ValidateBookNowModel(bookNow);

                if (ModelState.IsValid && !errors.HasValue)
                {
                    // do logic
                    List<DateTime> requiredDates = new List<DateTime>();

                    DateTime startRef = bookNow.Start.Date;
                    while (startRef <= bookNow.End)
                    {
                        requiredDates.Add(startRef);
                        startRef = startRef.AddDays(1);
                    }

                    DateTime holdExpires = DateTime.Now.ToUniversalTime().AddMinutes(ReserveController.HoldLength);

                    List<Hold> holds = db.Dates.Where(i => requiredDates.Contains(i.Date1) && i.IsActive).ToList()
                        .Select(i => new Hold()
                        {
                            DateID = i.DateID,
                            Expiration = holdExpires
                        })
                        .ToList();

                    if (holds.Count() < requiredDates.Count())
                    {
                        ViewBag.ErrorCode = Errors.BookNow_SomeDatesUnavailable;
                    }

                    DateTime expiration = DateTime.Now.ToUniversalTime();

                    var currentlyReserved = db.ReservationDates.Where(i => requiredDates.Contains(i.Date.Date1)).Select(i => i.Date.Date1).Distinct().Count();
                    var check = db.Holds.Where(i => requiredDates.Contains(i.Date.Date1) && expiration <= i.Expiration).Count();

                    if (check > 0 || currentlyReserved > 0)
                    {
                        // one or more of the days selected is already being held
                        ViewBag.ErrorCode = Errors.BookNow_SelectedDatesOnHold;
                    }
                    else
                    {
                        // hold for this user.
                        var dates = db.Holds.AddRange(holds).ToList();
                        db.SaveChanges();

                        Session["Hold"] = new TemporaryReservationViewModel()
                        {
                            Holds = dates,
                            GuestCount = bookNow.Guests
                        };

                        return RedirectToAction("Index", "Reserve");
                    }
                }
                else
                {
                    if (errors.HasValue)
                    {
                        ViewBag.ErrorCode = errors.Value;
                    }
                    else
                    {
                        ViewBag.ErrorCode = Errors.BookNow_OtherError;
                    }
                }
            }

            ViewBag.AvailableDates = GetAvailableDates();

            return View();
        }

        public ActionResult Amenities()
        {
            return View();
        }

        public ActionResult Photos()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
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