using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebStore.Models;

namespace WebStore.Controllers
{
    public class ProductController : Controller
    {
        private StoreDbContext db = new StoreDbContext();

        // GET: Product
        public ActionResult Index()
        {
            var products = db.Product.Include(p => p.Category).ToList();
            return View(products);
        }

        // GET: Product/Create
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName");
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        public ActionResult Create(Product product, IEnumerable<HttpPostedFileBase> ImageList)
        {
            if (ModelState.IsValid)
            {
                // Set version based on user input

                // Process the uploaded files
                if (ImageList != null && ImageList.Any())
                {
                    var imagePaths = new List<string>();

                    foreach (var file in ImageList)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                            file.SaveAs(path);
                            imagePaths.Add(fileName);
                        }
                    }

                    // Set the ImageList property to the saved file names
                    product.ImageList = string.Join(",", imagePaths);
                }

                db.Product.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Return to the same view with validation errors
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: Product/Edit/5
        public ActionResult Edit(int id)
        {
            var product = db.Product.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        public ActionResult Edit(Product product, IEnumerable<HttpPostedFileBase> ImageList)
        {
            if (ModelState.IsValid)
            {

                // Process the uploaded files
                if (ImageList != null && ImageList.Any())
                {
                    var imagePaths = new List<string>();

                    foreach (var file in ImageList)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                            file.SaveAs(path);
                            imagePaths.Add(fileName);
                        }
                    }

                    // Set the ImageList property to the saved file names
                    product.ImageList = string.Join(",", imagePaths);
                }

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Return to the same view with validation errors
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: Product/Delete/5
        public ActionResult Delete(int id)
        {
            var product = db.Product.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Product.Find(id);
            db.Product.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    
    public ActionResult Details(int id)
        {
            var product = db.Product.Include(p => p.Category)
                                  .FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }
    } 
}