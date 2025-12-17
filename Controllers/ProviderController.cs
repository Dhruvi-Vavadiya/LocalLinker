using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalLinker.App_Data;
using LocalLinker.Models;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text;
//using static Org.BouncyCastle.Math.EC.ECCurve;

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
                //var completed = _context.Booking.Count(b => b.ProviderId == providerId && b.Status == "Completed");
                var completed = (from b in _context.Booking
       join sr in _context.ServiceRequests
           on b.Service_Request_Id equals sr.Request_id
       where b.ProviderId == providerId
             && b.Status == "Completed"
       select b).Count();
                //var pending = _context.Booking.Count(b => b.ProviderId == providerId && b.Status == "Pending");
                var pending = (from b in _context.Booking
                                 join sr in _context.ServiceRequests
                                     on b.Service_Request_Id equals sr.Request_id
                                 where b.ProviderId == providerId
                                       && b.Status == "Pending"
                               select b).Count();
                var Cancelled = (from b in _context.Booking
                               join sr in _context.ServiceRequests
                                   on b.Service_Request_Id equals sr.Request_id
                               where b.ProviderId == providerId
                                     && b.Status == "Cancelled"
                               select b).Count();

                ViewBag.TotalBookings = totalBookings;
                ViewBag.Completed = completed;
                ViewBag.Pending = pending;
                ViewBag.Cancelled = Cancelled;
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
                                where sr.Status == "Pending" || sr.Status == "Confirmed"
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
                int? providerId = HttpContext.Session.GetInt32("pUserId");
                if (providerId == null)
                {
                    return RedirectToAction("Login", "Provider"); // or handle unauthorized
                }
                // 1️⃣ Fetch the booking and confirm
                var servicerequest = _context.ServiceRequests
                             .FirstOrDefault(x => x.Request_id == requestId);

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

                //    var booking = _context.Booking
                //.FirstOrDefault(b => b.Service_Request_Id == requestId && b.ProviderId == providerId);


                if (status == "Confirmed" || status == "Cancelled")
                {
                    // ➕ Add new booking record
                    var booking = new Booking
                    {
                        Service_Request_Id = requestId,
                        ProviderId = providerId.Value,
                        Status = status,
                        Created_At = DateTime.Now
                    };

                    _context.Booking.Add(booking);
                }
                else if (status == "Completed")
                {
                    // 🔁 Update existing booking record
                    var booking = _context.Booking
                        .FirstOrDefault(b => b.Service_Request_Id == requestId
                                          && b.ProviderId == providerId);

                    if (booking != null)
                    {
                        booking.Status = "Completed";
                        booking.Modifiy_Date = DateTime.Now;
                        _context.Booking.Update(booking);
                    }
                }

                // 4️⃣ Save all changes
                _context.SaveChanges();

                // 3️⃣ Send email to customer notifying status update
                // Assuming servicerequest is your ServiceRequest object
                // Get providerId from session


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
                if (status == "Confirmed")
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
                    TempData["ToastMessage"] = $"Email sending for booking {sts} " + bookingDetails.user_email;
                    TempData["ToastType"] = "success"; // success | error | warning | info

                    Console.WriteLine("Sending email to: " + bookingDetails.user_email);

                }



                // Redirect back to AssignedBookings
                return RedirectToAction("AssignedBookings");
            }
            catch (Exception ex)
            {
                _dataLog.Log("Provider(UpdateStatus)", ex.Message);
                return RedirectToAction("AssignedBookings");
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
                        Image = dr["Image"] == DBNull.Value || string.IsNullOrWhiteSpace(dr["Image"].ToString())
        ? "default.png"
        : dr["Image"].ToString()
                    }
                ;
                    //if(provider.UserType != "Provider")
                    //{
                    //    TempData["ErrorMessage"] = "Your are not Provider";
                    //    TempData["ToastMessage"] = "Your are not Provider";
                    //    TempData["ToastType"] = "error"; // success | error | warning | info
                    //    return RedirectToAction("Login");
                    //}

                    int serviceId = (int)_context.ServiceProviders
                             .Where(sp => sp.User_id == provider.UserId)
                             .Select(sp => sp.Service_id)
                             .FirstOrDefault();


                    HttpContext.Session.SetInt32("pserviceId", serviceId);
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
            int? serviceId = HttpContext.Session.GetInt32("pserviceId");

            if (userId == null)
                return RedirectToAction("Login");

            // 2️⃣ Find Provider using UserId
            var provider = _context.ServiceProviders
                .FirstOrDefault(p => p.User_id == userId);

            if (provider == null)
                return NotFound("Provider not found");

            // 3️⃣ Load all services (for dropdown)
            var services = _context.Services
                .Select(s => new { s.Service_id, s.Service_name, s.Image })
                .ToList();

            ViewBag.Services = services;

            // 4️⃣ Send selected service to view
            ViewBag.ProviderId = provider.Provider_id;
            ViewBag.SelectedService = provider.Service_id;
            ViewBag.ExperienceYears = provider.Experience_years;

            ViewBag.Cities = _context.Location
                   .Select(l => l.City)
                   .Distinct()
                   .ToList();
            var serviceimage = _context.Services
                        .Where(s => s.Service_id == serviceId.Value)
                        .Select(s => s.Image)
                        .FirstOrDefault();
            if (serviceimage != null)
            {
                // service contains the image path or URL
                ViewBag.ServiceImage = serviceimage;
            }
            else
            {
                ViewBag.ServiceImage = "default.png"; // fallback image
            }

            return View(provider);
        }
        [HttpGet]
        public JsonResult GetAreasByCity(string city)
        {
            var areas = _context.Location
                        .Where(l => l.City == city)
                        .Select(l => new
                        {
                            location_id = l.Location_id,
                            area = l.Area
                        })
                        .ToList();

            return Json(areas);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(Models.ServiceProvider model, int Location_id)
        {
            try
            {
                _conn = _inc.db_locallinker(_config);
                _cmd.Connection = _conn;
                _conn.Open();

                _cmd.CommandText = "UPDATE ServiceProviders SET Service_id = @serviceId,experience_years = @experience,location_id = @locationId,description = @description WHERE Provider_id = @providerId";
                _cmd.Parameters.Clear();
                _cmd.CommandType = CommandType.Text;
                _cmd.Parameters.AddWithValue("@serviceId",
                              model.Service_id ?? (object)DBNull.Value);

                _cmd.Parameters.AddWithValue("@experience",
                    model.Experience_years ?? 0); // default 0 if null

                _cmd.Parameters.AddWithValue("@locationId", Location_id);

                _cmd.Parameters.AddWithValue("@description",
                    model.Description ?? (object)DBNull.Value);

                _cmd.Parameters.AddWithValue("@providerId", model.Provider_id);

                _cmd.ExecuteNonQuery();

                return RedirectToAction("Dashboard", new { providerId = model.Provider_id });
            }catch(Exception ex) {
                _dataLog.Log("Provider(updateprofile)", ex.Message);
                TempData["ErrorMessage"] = "An error occurred during update profile. Please try again.";
                return View(model);
            }
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
            HttpContext.Session.SetString("Services", JsonConvert.SerializeObject(services));
            //ViewBag.Services = services;
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
                var user = new users
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Password = model.Password,
                    UserType = "Provider",
                    Is_Active = true,
                    CreatedAt = DateTime.Now

                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // 2️⃣ Get Service_id using service name
                var service = _context.Services
                    .FirstOrDefault(s => s.Service_name == model.UserType);

                // 3️⃣ Insert into ServiceProvider table
                var provider = new Models.ServiceProvider
                {
                    User_id = user.User_id,
                    Service_id = service?.Service_id, // may be null-safe
                    Location_id = null, // ✅ allowed
                    IsVerified = false
                };

                _context.ServiceProviders.Add(provider);
                _context.SaveChanges();
                HttpContext.Session.SetInt32("pUserId", user.User_id);
                HttpContext.Session.SetString("pUserEmail", user.Email);
                HttpContext.Session.SetString("pUserName", user.Name);
                HttpContext.Session.SetString("pUserRole", user.UserType);
                HttpContext.Session.SetString("pUserPhone", user.Phone);
                //HttpContext.Session.SetString("pUserImage", provider.Image);

                TempData["SuccessMessage"] = "Registration successful! Please login to continue.";
                return RedirectToAction("Dashboard");
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

        public IActionResult ProviderEarningsReport(DateTime? fromDate, DateTime? toDate)
        {
            int? userId = HttpContext.Session.GetInt32("pUserId");

            if (userId == null)
                return RedirectToAction("Login");
            // Default: last 30 days
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var report = (from b in _context.Booking
                          join sr in _context.ServiceRequests
                              on b.Service_Request_Id equals sr.Request_id
                          join u in _context.Users
                              on b.ProviderId equals u.User_id
                          where b.Status == "Completed"
                                && b.Created_At >= fromDate
                                && b.Created_At <= toDate
                                && b.ProviderId == userId
                          select new
                          {
                              b.BookingId,
                              b.Created_At,
                              b.Amount,
                              ProviderName = u.Name,
                              ServiceRequestId = sr.Request_id
                          }).ToList<dynamic>();

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            ViewBag.TotalBookings = report.Count;
            ViewBag.TotalEarnings = report.Sum(x => (decimal)(x.Amount ?? 0));


            return View(report);
        }

        public IActionResult EditPersonalInfo()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("pUserId"));

            var user = _context.Users.FirstOrDefault(u => u.User_id == userId);
            if (user != null && string.IsNullOrEmpty(user.Image))
            {
                user.Image = "default.png";
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult EditPersonalInfo(users model, IFormFile ProfileImage)
        {
            var user = _context.Users.FirstOrDefault(u => u.User_id == model.User_id);

            if (user != null)
            {
                user.Name = model.Name;
                user.Phone = model.Phone;

                // Image upload
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(ProfileImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ProfileImage.CopyTo(stream);
                    }

                    user.Image = fileName; // save filename in DB
                }
                //HttpContext.Session.SetInt32("pUserId", user.UserId);
                //HttpContext.Session.SetString("pUserEmail", provider.Email);
                HttpContext.Session.SetString("pUserName", user.Name);
                //HttpContext.Session.SetString("pUserRole", provider.UserType);
                HttpContext.Session.SetString("pUserPhone", user.Phone);
                HttpContext.Session.SetString("pUserImage", user.Image);
                _context.SaveChanges();
            }

            return RedirectToAction("EditPersonalInfo");
        }


        //=================================

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