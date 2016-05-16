using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SalmonRiver;

namespace SalmonRiver.Controllers
{
    public class ReserveController : Controller
    {
        private SalmonRiverEntities db = new SalmonRiverEntities();

        // GET: Reserve
        public ActionResult Index()
        {
            var holds = db.Holds.Include(h => h.Date);
            return View(holds.ToList());
        }

        [HttpPost]
        public ActionResult Index(DateTime start, DateTime end, int guests)
        {
            List<DateTime> requiredDates = new List<DateTime>();

            DateTime startRef = start.Date;
            while (startRef <= end)
            {
                requiredDates.Add(startRef);
                startRef = startRef.AddDays(1);
            }

            DateTime expiration = DateTime.Now.ToUniversalTime();

            var check = db.Holds.Where(i => requiredDates.Contains(i.Date.Date1) && expiration <= i.Expiration).Count();

            if (check > 0)
            {
                // one or more of the days selected is already being held

                ViewBag.Error = "BEING_HELD";
                return View();
            }
            else
            {
                return View();
            }
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
