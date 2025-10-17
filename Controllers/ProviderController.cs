using System.Text;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;
using LocalLinker.App_Data;
using MySql.Data.MySqlClient;

namespace LocalLinker.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IDataLog _dataLog;
        MySqlConnection _conn;
        MySqlCommand _cmd = new MySqlCommand();
        Include _inc = new Include();
        public ProviderController(ApplicationDbContext context, IConfiguration config, IDataLog dataLog)
        {
            _context = context;
            _config = config;
            _dataLog = dataLog;
        }

        // Provider dashboard
        public IActionResult Dashboard(int providerId)
        {
            try
            {
                var totalBookings = _context.Booking.Count(b => b.ProviderId == providerId);
                var completed = _context.Booking.Count(b => b.ProviderId == providerId && b.Status == "Completed");
                var pending = _context.Booking.Count(b => b.ProviderId == providerId && b.Status == "Pending");

                ViewBag.TotalBookings = totalBookings;
                ViewBag.Completed = completed;
                ViewBag.Pending = pending;
                return View();
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(Dashboard)", ex.Message);
            }
            return View();
           
        }

        // Assigned bookings list
        public IActionResult AssignedBookings(int providerId)
        {
            try
            {
                var bookings = _context.Booking
               .Where(b => b.ProviderId == providerId)
               .ToList();
                return View(bookings);
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(AssignedBookings)", ex.Message);
                TempData["msg"] = "Error fetching bookings: " + ex.Message;
                return View(new List<Booking>());
            }
        }

        // Update status
        [HttpPost]
        public IActionResult UpdateStatus(int bookingId, string status)
        {
            try
            {
                var booking = _context.Booking.Find(bookingId);
                if (booking != null)
                {
                    booking.Status = status;
                    booking.Modifiy_Date = DateTime.Now;

                    _context.SaveChanges();
                }
                return RedirectToAction("AssignedBookings", new { providerId = booking.ProviderId });
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(UpdateStatus)", ex.Message);
            }
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }





        // GET: /Account/Login
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Dashboard", "Provider");
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            
            try
            {
                // Hash the password for comparison
                var hashedPassword = HashPassword(password);

                //// Find provider by email and password
                //var provider = _context.Users
                //    .FirstOrDefault(p => p.Email == email && p.Password == password);
                _conn = _inc.db_locallinker(_config);
                _cmd.Connection = _conn;
                _conn.Open();

                _cmd.CommandText = "SELECT * FROM users WHERE email = @Email AND password = @Password";
                _cmd.Parameters.Clear();
                _cmd.Parameters.AddWithValue("@Email", email);
                _cmd.Parameters.AddWithValue("@Password", password);

                MySqlDataReader dr = _cmd.ExecuteReader();

                if (dr.Read())
                {

                    var provider = new
                    {
                        UserId = Convert.ToInt32(dr["User_id"]),
                        Name = dr["name"].ToString(),
                        Email = dr["email"].ToString(),
                        UserType = dr["UserType"].ToString(),
                        is_Active = dr["Is_Active"].ToString()
                    };
                    if (provider.is_Active == "No")
                    {
                        TempData["ErrorMessage"] = "Your account has been deactivated. Please contact support.";
                        return RedirectToAction("Login");
                    }
                    HttpContext.Session.SetInt32("UserId", provider.UserId);
                    HttpContext.Session.SetString("UserEmail", provider.Email);
                    HttpContext.Session.SetString("UserName", provider.Name);
                    HttpContext.Session.SetString("UserRole", provider.UserType);


                    _conn.Close();
                    _dataLog.Log("Provider(login)", "Provider Login suucessfully");
                    TempData["SuccessMessage"] = $"Welcome back, {provider.Name}!";
                    return RedirectToAction("Dashboard", "Provider", new { providerId = provider.UserId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid email or password. Please try again.";
                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _dataLog.Log("Provider(login)", ex.Message);
                TempData["ErrorMessage"] = "An error occurred during login. Please try again.";
                return RedirectToAction("Login");
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Clear remember me cookie
            Response.Cookies.Delete("RememberMe");
            _dataLog.Log("Provider(logout)", "Provider Logout suucessfully");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Provider");
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(users model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // In a real application, you would send an email with a password reset link
            // For now, we'll just show a success message
            TempData["SuccessMessage"] = "If an account with that email exists, we've sent password reset instructions.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            List<Service> services = new List<Service>();


            _conn = _inc.db_locallinker(_config);
            _cmd.Connection = _conn;
            _conn.Open();
            _cmd.CommandText = "SELECT Service_name FROM services";
            MySqlDataReader dr = _cmd.ExecuteReader();
           
            while (dr.Read())
            {
                var ser = new LocalLinker.Models.Service
                {
                    Service_name = dr["Service_name"].ToString()
                };
                services.Add(ser);
            }
            ViewBag.Services = services;
            return View(new users());


            //return View(services);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(users model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email already exists
                if (_context.Users.Any(p => p.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email address is already registered.");
                    return View(model);
                }

                // Create new provider
                var provider = new users
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = HashPassword(model.Password),
                    Phone = model.Phone,
                    UserType = model.UserType,
                    Is_Active = true,
                    CreatedAt = DateTime.Now

                };

                _context.Users.Add(provider);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login to continue.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(Register)", ex.Message);
                TempData["ErrorMessage"] = "An error occurred during registration. Please try again.";
                return View(model);
            }
        }

        // Helper method to hash passwords
        private string HashPassword(string password)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(HashPassword)", ex.Message);
            }
              return string.Empty;
        }

        // Auto-login from remember me cookie
        //public IActionResult AutoLogin()
        //{
        //    if (HttpContext.Request.Cookies.TryGetValue("RememberMe", out string providerIdStr))
        //    {
        //        if (int.TryParse(providerIdStr, out int providerId))
        //        {
        //            var provider = _context.Users.Find(providerId);
        //            if (provider != null && provider.Is_Active)
        //            {
        //                HttpContext.Session.SetInt32("UserId", provider.User_id);
        //                HttpContext.Session.SetString("UserEmail", provider.Email);
        //                HttpContext.Session.SetString("UserName", provider.Name);
        //                HttpContext.Session.SetString("UserRole", "Provider");

        //                return RedirectToAction("Dashboard", "Provider", new { providerId = provider.User_id });
        //            }
        //        }
        //    }

        //    return RedirectToAction("Login");
        //}
    }
}

//💡 Step 5: What You Can Implement
//✅ Admin Panel

//Manage Users (CRUD)

//Approve/Reject Providers

//Manage Services

//View Bookings, Reviews

//✅ Customer Portal

//Browse Services

//Send Request

//Make Booking

//Add Review

//✅ Provider Dashboard

//View Assigned Requests

//Accept/Reject Jobs

//Update Job Status

//View Reviews