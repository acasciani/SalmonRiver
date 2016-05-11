using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SalmonRiver;
using PagedList;

namespace SalmonRiver.Controllers
{
    public class DatesController : Controller
    {
        private SalmonRiverEntities db = new SalmonRiverEntities();

        // GET: Dates
        public ActionResult Index(int? page, bool? showPast, DateTime? startDate, DateTime? endDate)
        {
            bool showPastDates = showPast ?? false;
            DateTime showStartDate = startDate ?? DateTime.MinValue;
            DateTime showEndDate = endDate ?? DateTime.MaxValue;

            var dates = db.Dates.Where(i=> (showPast == true || DateTime.Today <= i.Date1) && showStartDate.Date <= i.Date1 && i.Date1 <= showEndDate.Date).Include(d => d.Holiday).OrderBy(i => i.Date1);

            int pageSize = 20;
            int pageNumber = page ?? 1;

            return View(dates.ToPagedList(pageNumber, pageSize));
        }

        // GET: Dates/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Date date = db.Dates.Find(id);
            if (date == null)
            {
                return HttpNotFound();
            }
            ViewBag.HolidayID = new SelectList(db.Holidays, "HolidayID", "Holiday1", date.HolidayID);
            return View(date);
        }

        // POST: Dates/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DateID,Date1,HolidayID,CheckIn,CheckOut,IsActive")] Date date)
        {
            if (ModelState.IsValid)
            {
                db.Entry(date).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.HolidayID = new SelectList(db.Holidays, "HolidayID", "Holiday1", date.HolidayID);
            return View(date);
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
