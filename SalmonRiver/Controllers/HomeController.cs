using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SalmonRiver.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(int? error)
        {
            ViewBag.ErrorCode = error.HasValue ? (Errors)error.Value : (Errors?)null;

            return View();
        }

        public ActionResult Amenities()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

    }
}