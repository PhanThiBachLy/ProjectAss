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
            ViewBag.Categories = _context.Category.ToList();
            var products = _context.Product.Include(p => p.Category).ToList(); // Initial load
            return View(products);
        }

        

        private string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
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
        [HttpPost]
        public JsonResult GetProductList(string searchTerm, string sortOrder, int? categoryId, int? page = 1)
        {
            var pageSize = 8;
            var query = _context.Product.Include(p => p.Category).AsQueryable();

            // Apply search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) ||
                                       p.Description.Contains(searchTerm));
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "priceAsc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "priceDesc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "nameAsc":
                    query = query.OrderBy(p => p.ProductName);
                    break;
                case "nameDesc":
                    query = query.OrderByDescending(p => p.ProductName);
                    break;
                case "ratingDesc":
                    query = query.OrderByDescending(p => p.Rating);
                    break;
                default:
                    query = query.OrderBy(p => p.ProductId);
                    break;
            }

            var products = query
                .Skip((page.Value - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var html = RenderRazorViewToString("_ProductList", products);

            return Json(new { html = html });
        }

        private string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngineCollection.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}