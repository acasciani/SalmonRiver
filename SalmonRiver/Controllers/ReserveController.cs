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

namespace SalmonRiver.Controllers
{
    public class ReserveController : Controller
    {
        public const int HoldLength = 15; // minutes

        private SalmonRiverEntities db = new SalmonRiverEntities();

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
