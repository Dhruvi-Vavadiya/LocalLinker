using System.Text;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
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
        public IActionResult AssignedBookings()
        {
            try
            {
                var bookings = (from sr in _context.ServiceRequests
                                join u in _context.Users
                                    on sr.Customer_id equals u.User_id
                                join s in _context.Services
                                    on sr.Service_id equals s.Service_id
                                join l in _context.Location
                                    on sr.Location_id equals l.Location_id
                                    where sr.Status == "Pending"
                                select new
                                {
                                    sr.Request_id,
                                    CustomerName = u.Name,
                                    ServiceName = s.Service_name,
                                    LocationName = l.Area, // Or City if you want
                                    sr.Status,
                                    sr.Description,
                                    EntryDate = sr.Entry_Date,
                                    ModifyDate = sr.Modifiy_Date
                                }).ToList<dynamic>();

                return View(bookings);
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(AssignedBookings)", ex.Message);
                TempData["msg"] = "Error fetching bookings: " + ex.Message;
                return View(new List<Booking>());
            }
        }
        public IActionResult Reviews()
        {
            try
            {
                var data = (from r in _context.Reviews
                            join sr in _context.ServiceRequests
                                on r.Service_Request_Id equals sr.Request_id
                            join u in _context.Users
                                on sr.Customer_id equals u.User_id
                            join s in _context.Services
                                on sr.Service_id equals s.Service_id
                            select new
                            {
                                r.Review_id,
                                r.Rating,
                                r.Review_Text,
                                r.Created_At,

                                ServiceRequestId = sr.Request_id,
                                CustomerName = u.Name,
                                ServiceName = s.Service_name
                            }).ToList<dynamic>();

                return View(data);
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(AssignedBookings)", ex.Message);
                TempData["msg"] = "Error fetching bookings: " + ex.Message;
                return View(new List<Booking>());
            }
        }

        [HttpPost]
        public IActionResult UpdateStatus(int requestId, string status)
        {
            try
            {
                // 1️⃣ Fetch the booking and confirm
                var servicerequest = _context.ServiceRequests
                             .FirstOrDefault(x => x.Request_id == requestId );

                if (servicerequest == null)
                {
                    return NotFound();
                }

                servicerequest.Status = status;
                servicerequest.Modifiy_Date = DateTime.Now;
                _context.ServiceRequests.Update(servicerequest);
                _context.SaveChanges();
                // 2️⃣ Optional: If you want to perform additional operations like
                // adding a booking record in another table, do it here
                // Example: add to a BookingHistory table

                var history = new Booking
                {
                    Service_Request_Id = servicerequest.Request_id,
                    Status = status,
                    ProviderId = HttpContext.Session.GetInt32("pUserId"),
                    Created_At = DateTime.Now
                };
                _context.Booking.Add(history);


                _context.SaveChanges(); // Save changes for status update (and history if added)

                // 3️⃣ Send email to customer notifying status update
                // Assuming servicerequest is your ServiceRequest object
                // Get providerId from session
                int? providerId = HttpContext.Session.GetInt32("pUserId");
                if (providerId == null)
                {
                    return RedirectToAction("Login", "Provider"); // or handle unauthorized
                }

                // Query to get assigned bookings for this provider
                var bookingDetails = (from sr in _context.ServiceRequests
                                      join s in _context.Services
                                          on sr.Service_id equals s.Service_id
                                          join u in _context.Users
                                          on sr.Customer_id equals u.User_id
                                      join l in _context.Location
                                          on sr.Location_id equals l.Location_id
                                      where sr.Request_id == servicerequest.Request_id
                                      select new
                                      {
                                          ServiceRequestId = sr.Request_id,
                                          ServiceId = s.Service_id,
                                          ServiceName = s.Service_name,
                                          LocationId = l.Location_id,
                                          LocationName = l.City + " " + l.Area,
                                          user_id = u.User_id,
                                          username = u.Name,
                                          user_email = u.Email
                                      }).FirstOrDefault();

                string sts = "";
                if(status == "Confirmed")
                {
                    sts = "Confirmed";
                }
                else
                {
                    sts = "Completed";
                }

                if (bookingDetails != null)
                {
                    string subject = $"Your Booking #{bookingDetails.ServiceRequestId} is {sts}";
                    string body = $"Hello {bookingDetails.username},<br/><br/>" +
                                  $"Your booking for {bookingDetails.ServiceName} at {bookingDetails.LocationName} is now <b>{sts}</b>.<br/><br/>" +
                                  $"Thank you for using our service!";

                    SendEmail(bookingDetails.user_email, subject, body);

                    Console.WriteLine("Sending email to: " + bookingDetails.user_email);

                }



                // Redirect back to AssignedBookings
                return RedirectToAction("AssignedBookings");
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(UpdateStatus)", ex.Message);
                return View();
            }
        }
        private readonly string _smtpServer = "smtp.gmail.com"; // e.g., smtp.gmail.com
        private readonly int _smtpPort = 587; // or 465
        private readonly string _fromEmail = "dhruvivavadiya2004@gmail.com";
        private readonly string _password = "dpds jhzj zoyt lpab";

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.Credentials = new NetworkCredential(_fromEmail, _password);
                        client.EnableSsl = true; // Use SSL if your server requires
                        client.Timeout = 15000; // ⏱ 15 seconds (IMPORTANT)
                        await client.SendMailAsync(message);
                        _dataLog.Log("Provider(AssignedBookings(SendEmail))", "mail is sended from " + toEmail);
                    }
                }
            }
            catch (SmtpException ex)
            {
                // Log this
                _dataLog.Log("Provider(AssignedBookings(SendEmail))", ex.Message);
                Console.WriteLine(ex.Message);
            }

        }

        public IActionResult Index()
        {
            return View();
        }





        // GET: /Account/Login
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("pUserId") != null)
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

                _cmd.CommandText = "SELECT * FROM users WHERE email = @Email AND password = @Password AND UserType='Provider'";
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
                        Is_Active = Convert.ToBoolean(dr["Is_Active"]),
                        Phone = dr["Phone"].ToString(),
                        Image = dr["Image"].ToString()
                    };
                    //if(provider.UserType != "Provider")
                    //{
                    //    TempData["ErrorMessage"] = "Your are not Provider";
                    //    TempData["ToastMessage"] = "Your are not Provider";
                    //    TempData["ToastType"] = "error"; // success | error | warning | info
                    //    return RedirectToAction("Login");
                    //}
                    if (!provider.Is_Active)
                    {
                        TempData["ErrorMessage"] = "Your account has been deactivated. Please contact support.";
                        return RedirectToAction("Login");
                    }
                    HttpContext.Session.SetInt32("pUserId", provider.UserId);
                    HttpContext.Session.SetString("pUserEmail", provider.Email);
                    HttpContext.Session.SetString("pUserName", provider.Name);
                    HttpContext.Session.SetString("pUserRole", provider.UserType);
                    HttpContext.Session.SetString("pUserPhone", provider.Phone);
                    HttpContext.Session.SetString("pUserImage", provider.Image);



                    _conn.Close();
                    _dataLog.Log("Provider(login)", "Provider Login suucessfully");
                    TempData["SuccessMessage"] = $"Welcome back, {provider.Name}!";
                    return RedirectToAction("Dashboard", "Provider", new { providerId = provider.UserId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid email or password or Your are not Provider. Please try again.";
                    //TempData["ToastMessage"] = "Invalid email or password or Your are not Provider. Please try again";
                    //TempData["ToastType"] = "error"; // success | error | warning | info
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



        [HttpGet]
        public ActionResult UpdateProfile()
        {
            // 1️⃣ Get UserId from Session
            int? userId = HttpContext.Session.GetInt32("pUserId");

            if (userId == null)
                return RedirectToAction("Login");

            // 2️⃣ Find Provider using UserId
            var provider = _context.ServiceProviders
                .FirstOrDefault(p => p.User_id == userId);

            if (provider == null)
                return NotFound("Provider not found");

            // 3️⃣ Load all services (for dropdown)
            var services = _context.Services
                .Select(s => new { s.Service_id, s.Service_name })
                .ToList();

            ViewBag.Services = services;

            // 4️⃣ Send selected service to view
            ViewBag.SelectedService = provider.Service_id;

            return View(provider);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(Models.ServiceProvider model)
        {
            var sql = @"UPDATE ServiceProviders 
                SET Service_id = @serviceId, 
                    experience_years = @experience, 
                    location_id = @locationId,
                    description = @description
                WHERE Provider_id = @providerId";

            _context.Database.ExecuteSqlRaw(sql,
                new MySqlParameter("@serviceId", model.Service_id ?? (object)DBNull.Value),
                new MySqlParameter("@experience", model.Experience_years ?? (object)DBNull.Value),
                new MySqlParameter("@locationId", model.Location_id ?? (object)DBNull.Value),
                new MySqlParameter("@description", model.Description ?? (object)DBNull.Value),
                new MySqlParameter("@providerId", model.Provider_id));

            return RedirectToAction("Dashboard", new { providerId = model.Provider_id });
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

        public IActionResult ProviderEarningsReport(
    int providerId,
    DateTime? fromDate,
    DateTime? toDate)
        {
            // Default date range (last 30 days)
            fromDate ??= DateTime.Now.AddDays(-30);
            toDate ??= DateTime.Now;

            var report = (from b in _context.Booking
                          join sr in _context.ServiceRequests
                              on b.Service_Request_Id equals sr.Request_id
                          join u in _context.Users
                              on b.ProviderId equals u.User_id
                          where b.ProviderId == providerId
                                && b.Status == "Completed"
                                && b.Created_At >= fromDate
                                && b.Created_At <= toDate
                          select new
                          {
                              b.BookingId,
                              b.Created_At,
                              b.Amount,
                              ServiceRequestId = sr.Request_id
                          }).ToList();

            ViewBag.ProviderName = _context.Users
                .Where(x => x.User_id == providerId)
                .Select(x => x.Name)
                .FirstOrDefault();

            ViewBag.TotalBookings = report.Count;
            ViewBag.TotalEarnings = report.Sum(x => x.Amount);

            return View(report);
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