using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace WebApplication2.Controllers
{
    [Authorize(Roles ="admin")]
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            foreach(var c in ClaimsPrincipal.Current.Claims)
            {

            }
            return View();
        }
        [Authorize(Roles ="admin, clerk")]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}