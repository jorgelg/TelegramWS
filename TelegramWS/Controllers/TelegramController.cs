using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TelegramWS.Controllers
{
    public class TelegramController : Controller
    {
        // GET: Telegram
        public TelegramController()
        {

        }

        public ActionResult Index()
        {
            return View();
        }
    }
}