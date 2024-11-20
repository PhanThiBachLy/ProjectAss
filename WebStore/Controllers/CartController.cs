using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebStore.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace WebStore.Controllers
{
    public class CartController : Controller
    {
        private readonly StoreDbContext _context;

        public CartController()
        {
            _context = new StoreDbContext();
        }

        public ActionResult Index()
        {
            int? userId = Session["UserID"] as int?;
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy giỏ hàng của user
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .Include("CartDetail.Product")
                .FirstOrDefault(c => c.UserId == userId);

            return View(cart);
        }
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();

            // Find or create cart for user
            var cart = _context.Cart
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                // Get max CartID and add 1
                var maxCartId = _context.Cart.Any() ? _context.Cart.Max(c => c.CartId) : 0;

                cart = new Cart
                {
                    CartId = maxCartId + 1,
                    UserId = userId,
                    TotalPayment = 0
                };
                _context.Cart.Add(cart);
                _context.SaveChanges();
            }

            // Get product
            var product = _context.Product.Find(productId);
            if (product == null) return HttpNotFound();

            // Check if product already exists in cart
            var existingCartDetail = _context.CartDetail
                .FirstOrDefault(cd => cd.CartId == cart.CartId && cd.ProductId == productId);

            if (existingCartDetail != null)
            {
                // Update quantity and total amount if product already exists
                existingCartDetail.Quantity += quantity;
                existingCartDetail.TotalAmount = existingCartDetail.UnitPrice * existingCartDetail.Quantity;
            }
            else
            {
                // Get max CartDetailID and add 1
                var maxCartDetailId = _context.CartDetail.Any() ?
                    _context.CartDetail.Max(cd => cd.CartDetailId) : 0;

                // Add new cart detail if product doesn't exist
                var cartDetail = new CartDetail
                {
                    CartDetailId = maxCartDetailId + 1,
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    TotalAmount = product.Price * quantity,
                    Status = "Pending"
                };
                _context.CartDetail.Add(cartDetail);
            }

            // Update cart total
            cart.TotalPayment = _context.CartDetail
                .Where(cd => cd.CartId == cart.CartId)
                .Sum(cd => cd.TotalAmount);

            _context.SaveChanges();

            // Get total items in cart for updating UI
            var cartItemCount = _context.CartDetail
                .Where(cd => cd.CartId == cart.CartId)
                .Sum(cd => cd.Quantity);

            return Json(new
            {
                success = true,
                cartCount = cartItemCount
            }, JsonRequestBehavior.AllowGet);
        
        }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    message = "Please login to add items to cart"
                }, JsonRequestBehavior.AllowGet);
            }
            }

            [HttpPost]
        public ActionResult BuyNow(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();

            // Get max OrderID and add 1
            var maxOrderId = _context.OrderItem.Any() ?
                _context.OrderItem.Max(o => o.OrderId) : 0;

            // Create order
            var order = new OrderItem
            {
                OrderId = maxOrderId + 1,
                UserId = userId,
                CreatedDate = DateTime.Now
            };

            // Get product
            var product = _context.Product.Find(productId);
            if (product == null) return HttpNotFound();

            // Get max OrderDetailID and add 1
            var maxOrderDetailId = _context.OrderDetail.Any() ?
                _context.OrderDetail.Max(od => od.OrderDetailId) : 0;

            // Create order detail
            var orderDetail = new OrderDetail
            {
                OrderDetailId = maxOrderDetailId + 1,
                OrderId = order.OrderId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalAmount = product.Price * quantity
            };

            // Calculate totals
            order.TotalAmount = orderDetail.TotalAmount;
            order.ShippingFee = 0; // Implement shipping fee calculation
            order.GrandTotal = order.TotalAmount + order.ShippingFee;

            _context.OrderItem.Add(order);
            _context.OrderDetail.Add(orderDetail);
            _context.SaveChanges();

            return RedirectToAction("Checkout", new { orderId = order.OrderId });
        }

        private int GetCurrentUserId()
        {
            var userId = HttpContext.Session["UserId"] as int?;
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Please login to continue");
            }
            return userId.Value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpPost]
        public ActionResult UpdateQuantity(int cartDetailId, int quantity)
        {
            var cartDetail = _context.CartDetail.Find(cartDetailId);
            if (cartDetail == null)
            {
                return Json(new { success = false, message = "Cart detail not found" });
            }

            // Kiểm tra số lượng hợp lệ
            if (quantity < 1)
            {
                return Json(new { success = false, message = "Invalid quantity" });
            }

            // Kiểm tra tồn kho


            cartDetail.Quantity = quantity;
            cartDetail.TotalAmount = cartDetail.UnitPrice * quantity;

            // Cập nhật tổng tiền trong giỏ hàng
            var cart = _context.Cart.Find(cartDetail.CartId);
            cart.TotalPayment = _context.CartDetail
                .Where(cd => cd.CartId == cart.CartId)
                .Sum(cd => cd.TotalAmount);

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                totalAmount = cartDetail.TotalAmount,
                cartTotal = cart.TotalPayment,
                message = "Quantity updated successfully"
            });
        }

        [HttpPost]
        public ActionResult RemoveItem(int cartDetailId)
        {
            var cartDetail = _context.CartDetail.Find(cartDetailId);
            if (cartDetail == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            // Kiểm tra quyền xóa
            var userId = GetCurrentUserId();
            var cart = _context.Cart.Find(cartDetail.CartId);
            if (cart.UserId != userId)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var cartId = cartDetail.CartId;
            _context.CartDetail.Remove(cartDetail);

            // Cập nhật giỏ hàng
            cart.TotalPayment = _context.CartDetail
                .Where(cd => cd.CartId == cartId)
                .Sum(cd => cd.TotalAmount);

            _context.SaveChanges();

            // Lấy số lượng item còn lại trong giỏ
            var remainingItems = _context.CartDetail
                .Count(cd => cd.CartId == cartId);

            return Json(new
            {
                success = true,
                remainingItems = remainingItems,
                cartTotal = cart.TotalPayment,
                message = "Item removed successfully"
            });
        }

        [HttpPost]
        public ActionResult ClearCart()
        {
            var userId = GetCurrentUserId();
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                return Json(new { success = false, message = "Cart not found" });
            }

            // Xóa tất cả cart detail
            _context.CartDetail.RemoveRange(cart.CartDetail);

            // Reset cart
            cart.TotalPayment = 0;

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Cart cleared successfully"
            });
        }

        [HttpGet]
        public ActionResult GetCartSummary()
        {
            var userId = GetCurrentUserId();
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                return Json(new
                {
                    itemCount = 0,
                    totalAmount = 0
                }, JsonRequestBehavior.AllowGet);
            }

            var summary = new
            {
                itemCount = cart.CartDetail.Sum(cd => cd.Quantity),
                totalAmount = cart.TotalPayment
            };

            return Json(summary, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Checkout()
        {
            var userId = GetCurrentUserId();
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .Include("CartDetail.Product")
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartDetail.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index");
            }

            // Lấy danh sách voucher có hiệu lực
            var availableVouchers = _context.Voucher
                .Where(v => v.ExpiryDate >= DateTime.Today)
                .ToList();

            var checkoutViewModel = new CheckoutViewModel
            {
                Cart = cart,
                ShippingAddress = new ShippingAddress(),
                AvailableVouchers = availableVouchers
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessCheckout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var userId = GetCurrentUserId();
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .Include("CartDetail.Product")
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartDetail.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Tạo order mới
                    var maxOrderId = _context.OrderItem.Any() ?
                        _context.OrderItem.Max(o => o.OrderId) : 0;

                    var order = new OrderItem
                    {
                        OrderId = maxOrderId + 1,
                        UserId = userId,
                        MessageForSeller = model.MessageForSeller,
                        ShippingAddress = model.ShippingAddress.FullAddress,
                        VoucherId = model.VoucherId,
                        TotalAmount = cart.TotalPayment,
                        ShippingFee = CalculateShippingFee(model.ShippingAddress),
                        CreatedDate = DateTime.Now
                    };

                    // Tính giảm giá nếu có voucher
                    if (model.VoucherId.HasValue)
                    {
                        var voucher = _context.Voucher.Find(model.VoucherId);
                        if (voucher != null)
                        {
                            var discountAmount = order.TotalAmount * (voucher.DiscountAmount / 100m);
                            if (voucher.MaxDiscountValue.HasValue)
                            {
                                discountAmount = Math.Min((sbyte)discountAmount, voucher.MaxDiscountValue.Value);
                            }
                            order.TotalAmount -= discountAmount;
                        }
                    }

                    order.GrandTotal = order.TotalAmount + order.ShippingFee;
                    _context.OrderItem.Add(order);

                    // Tạo order details
                    foreach (var cartDetail in cart.CartDetail)
                    {
                        var maxOrderDetailId = _context.OrderDetail.Any() ?
                            _context.OrderDetail.Max(od => od.OrderDetailId) : 0;

                        var orderDetail = new OrderDetail
                        {
                            OrderDetailId = maxOrderDetailId + 1,
                            OrderId = order.OrderId,
                            ProductId = cartDetail.ProductId,
                            ProductType = cartDetail.ProductType,
                            Quantity = cartDetail.Quantity,
                            UnitPrice = cartDetail.UnitPrice,
                            TotalAmount = cartDetail.TotalAmount,
                            VoucherId = model.VoucherId
                        };
                        _context.OrderDetail.Add(orderDetail);

                        // Cập nhật số lượng sản phẩm trong kho
                        var product = _context.Product.Find(cartDetail.ProductId);

                    }

                    // Xóa cart details
                    _context.CartDetail.RemoveRange(cart.CartDetail);
                    cart.TotalPayment = 0;
                    cart.SelectedProductCount = 0;

                    _context.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Order placed successfully!";
                    return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Error processing your order: " + ex.Message);
                    return View("Checkout", model);
                }
            }
        }

        private decimal CalculateShippingFee(ShippingAddress address)
        {
            // Logic tính phí ship dựa trên địa chỉ
            return 30000; // Phí mặc định
        }

        public ActionResult OrderConfirmation(int orderId)
        {
            var order = _context.OrderItem
                .Include(o => o.OrderDetail)
                .Include("OrderDetail.Product")
                .Include(o => o.Voucher)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [HttpPost]
        public ActionResult CreateOrder([Bind(Include = "ShippingAddress,PhoneNumber,MessageForSeller,VoucherId")] OrderItem orderInput)
        {
            var userId = GetCurrentUserId();
            var cart = _context.Cart
                .Include(c => c.CartDetail)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartDetail.Any())
                return Json(new { success = false, message = "Cart is empty" });

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var maxOrderId = _context.OrderItem.Any() ?
                        _context.OrderItem.Max(o => o.OrderId) : 0;

                    var order = new OrderItem
                    {
                        OrderId = maxOrderId + 1,
                        UserId = userId,
                        ShippingAddress = orderInput.ShippingAddress,
                        MessageForSeller = orderInput.MessageForSeller,
                        VoucherId = orderInput.VoucherId,
                        TotalAmount = cart.TotalPayment,
                        ShippingFee = 30000m,
                        CreatedDate = DateTime.Now
                    };

                    if (orderInput.VoucherId.HasValue)
                    {
                        var voucher = _context.Voucher.Find(orderInput.VoucherId);
                        if (voucher != null)
                        {
                            decimal discountAmount = (decimal)(order.TotalAmount * (decimal)(voucher.DiscountAmount / 100.0m));
                            if (voucher.MaxDiscountValue.HasValue)
                            {
                                discountAmount = Math.Min(discountAmount, (decimal)voucher.MaxDiscountValue.Value);
                            }
                            order.TotalAmount -= discountAmount;
                        }
                    }

                    order.GrandTotal = order.TotalAmount + order.ShippingFee;
                    _context.OrderItem.Add(order);

                    foreach (var cartDetail in cart.CartDetail)
                    {
                        var maxOrderDetailId = _context.OrderDetail.Any() ?
                            _context.OrderDetail.Max(od => od.OrderDetailId) : 0;

                        var orderDetail = new OrderDetail
                        {
                            OrderDetailId = maxOrderDetailId + 1,
                            OrderId = order.OrderId,
                            ProductId = cartDetail.ProductId,
                            ProductType = cartDetail.ProductType,
                            Quantity = cartDetail.Quantity,
                            UnitPrice = cartDetail.UnitPrice,
                            TotalAmount = cartDetail.TotalAmount,
                            VoucherId = orderInput.VoucherId
                        };

                        _context.OrderDetail.Add(orderDetail);
                    }

                    _context.CartDetail.RemoveRange(cart.CartDetail);
                    cart.TotalPayment = 0;
                    cart.SelectedProductCount = 0;

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
    }
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; }
        public ShippingAddress ShippingAddress { get; set; }
        public string MessageForSeller { get; set; }
        public int? VoucherId { get; set; }
        public List<Voucher> AvailableVouchers { get; set; }
    }

    public class ShippingAddress
    {
        [Required]
        public string RecipientName { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string FullAddress { get; set; }
    }
}
