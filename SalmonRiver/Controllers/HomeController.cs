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

        public ActionResult Index(int? error)
        {
            DateTime expiration = DateTime.Now.ToUniversalTime();
            DateTime maxBookDate = DateTime.Today.AddMonths(6);

            List<DateTime> onHold = db.Holds.Where(i => i.Expiration < expiration).Select(i => i.Date.Date1).Distinct().ToList();
            List<DateTime> available = db.Dates.Where(i => i.IsActive && i.Date1 >= DateTime.Today && i.Date1 <= maxBookDate && !onHold.Contains(i.Date1)).Select(i => i.Date1).Distinct().ToList();

            ViewBag.AvailableDates = available;
            ViewBag.ErrorCode = error.HasValue ? (Errors)error.Value : (Errors?)null;

            return View();
        }

        [HttpPost]
        public ActionResult Index(BookNowViewModel bookNow)
        {
            if (ModelState.IsValid)
            {
                // now check to see that the model is actually passes validation
                using (ReserveController rc = new ReserveController())
                {
                    Errors? errors = rc.ValidateBookNowModel(bookNow);

                    if (errors.HasValue)
                    {
                        ViewBag.ErrorCode = errors.Value;
                    }
                    else
                    {
                        // do logic
                    }
                }
            }

            return View();
        }

        public ActionResult Amenities()
        {
            ViewBag.Message = "Your application description page.";

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