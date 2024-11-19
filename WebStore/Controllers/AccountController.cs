using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebStore.Models;

namespace WebStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly StoreDbContext _context;

        public AccountController()
        {
            _context = new StoreDbContext();
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                var user = _context.User.FirstOrDefault(u =>
                    u.Username == model.Username &&
                    u.PasswordHash == model.Password);

                if (user != null)
                {
                    // Create authentication ticket
                    var authTicket = new FormsAuthenticationTicket(
                        1,                              // version
                        user.Username,                  // user name
                        DateTime.Now,                   // created
                        DateTime.Now.AddMinutes(30),    // expires
                        model.RememberMe,               // persist cookie
                        user.Role                       // user data
                    );

                    // Encrypt the ticket
                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);

                    // Create cookie
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    if (model.RememberMe)
                    {
                        authCookie.Expires = authTicket.Expiration;
                    }
                    Response.Cookies.Add(authCookie);

                    // Store user info in session if needed
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["Role"] = user.Role;

                    // Redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password");
                }
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }


        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        
        public ActionResult Register(RegisterViewModel model)
        {
            
                if (_context.User.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }

                if (_context.User.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password, // Note: You should hash the password in production
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Role = "User",  // Default role for new registrations
                   
                };

                _context.User.Add(user);
                _context.SaveChanges();

                // Add success message to TempData (will persist through redirect)
                TempData["SuccessMessage"] = "Registration successful! Please login.";

                // Redirect to login page
                return RedirectToAction("Login", "Account");
            

            
        }

        [Authorize]
        public async Task<ActionResult> Profile()
        {
            var username = User.Identity.Name;
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return HttpNotFound();
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                return HttpNotFound();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Profile updated successfully";

            return RedirectToAction("Profile");
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                return HttpNotFound();
            }

            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password changed successfully";
            return RedirectToAction("Profile");
        }

        [Authorize]
        public ActionResult Logout()
        {
            System.Web.Security.FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // View Models
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.Web.Mvc.Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }
    }

    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}