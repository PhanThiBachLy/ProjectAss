using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebStore.Models;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace WebStore.Controllers
{
    public class HomeController : Controller
    {
        private StoreDbContext _context = new StoreDbContext();

        public ActionResult Index()
        {
            var products = _context.Product.Include(p => p.Category).ToList();
            return View(products);
        }

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