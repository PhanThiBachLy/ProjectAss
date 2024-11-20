using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebStore.Models;
using Microsoft.EntityFrameworkCore;
namespace WebStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly StoreDbContext _context = new StoreDbContext();



        // GET: Order/History/5 (userId)
        // GET: Order/History/5 (userId)
        public ActionResult History(int id)
        {
            var orders = _context.OrderItem
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Product)
                .Include(o => o.Voucher)
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();

            return View(orders);
        }

        // GET: Order/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var orderDetails = await _context.OrderDetail
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Include(od => od.Voucher)
                .Where(od => od.OrderId == id)
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return HttpNotFound();
            }

            ViewBag.OrderId = id;
            return View(orderDetails);
        }
    }
}