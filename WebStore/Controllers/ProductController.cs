using Microsoft.EntityFrameworkCore;
using System;
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
            var product = db.Product.Include(p => p.Category).FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, IEnumerable<HttpPostedFileBase> NewImages)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = db.Product.AsNoTracking()
                        .FirstOrDefault(p => p.ProductId == product.ProductId);

                    if (existingProduct == null)
                    {
                        return HttpNotFound();
                    }

                    // Xử lý ảnh mới nếu có
                    if (NewImages != null && NewImages.Any(f => f != null && f.ContentLength > 0))
                    {
                        // Xóa ảnh cũ nếu cần
                        if (!string.IsNullOrEmpty(existingProduct.ImageList))
                        {
                            foreach (var oldImage in existingProduct.ImageList.Split(','))
                            {
                                var oldPath = Path.Combine(Server.MapPath("~/Content/Images/"), oldImage);
                                if (System.IO.File.Exists(oldPath))
                                {
                                    System.IO.File.Delete(oldPath);
                                }
                            }
                        }

                        // Lưu ảnh mới
                        var imagePaths = new List<string>();
                        foreach (var file in NewImages)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                // Tạo tên file unique để tránh trùng lặp
                                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
                                var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);

                                // Kiểm tra kích thước và định dạng file
                                if (file.ContentLength > 5 * 1024 * 1024) // 5MB
                                {
                                    ModelState.AddModelError("", $"File {file.FileName} vượt quá kích thước cho phép (5MB)");
                                    continue;
                                }

                                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                                var fileExtension = Path.GetExtension(fileName).ToLower();
                                if (!allowedExtensions.Contains(fileExtension))
                                {
                                    ModelState.AddModelError("", $"File {file.FileName} không đúng định dạng");
                                    continue;
                                }

                                file.SaveAs(path);
                                imagePaths.Add(fileName);
                            }
                        }

                        if (imagePaths.Any())
                        {
                            product.ImageList = string.Join(",", imagePaths);
                        }
                    }
                    else
                    {
                        // Giữ nguyên danh sách ảnh cũ nếu không có ảnh mới
                        product.ImageList = existingProduct.ImageList;
                    }

                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
                }
            }

            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
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