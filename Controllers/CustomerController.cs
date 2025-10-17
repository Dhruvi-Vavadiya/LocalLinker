using LocalLinker.App_Data;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using MySql.Data.MySqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace LocalLinker.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IConfiguration _config;
        MySqlConnection _conn;
        MySqlCommand _cmd = new MySqlCommand();
        Include _inc = new Include();
        private readonly ApplicationDbContext _context;
        public CustomerController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Display booking form
        [HttpGet]
        public IActionResult MakeBooking()
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Services = _context.Services.ToList();
            ViewBag.Providers = _context.Users.Where(u => u.UserType == "Provider").ToList();
            return View();
        }

        // Submit booking with time selection
        [HttpPost]
        public IActionResult MakeBooking(Booking booking, DateTime bookingTime)
        {
            try
            {
                int? customerId = HttpContext.Session.GetInt32("UserId");

                if (customerId == null)
                {
                    return RedirectToAction("Login");
                }
                var dddd = bookingTime;


                booking.Status = "Pending";
                booking.Created_At = DateTime.Now;
                booking.Modifiy_Date = DateTime.Now;
                _context.Booking.Add(booking);
                _context.SaveChanges();
                TempData["msg"] = "Booking Created Successfully!";
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error creating booking: " + ex.Message;
            }
            return RedirectToAction("MakeBooking");

        }

        // Show all customer’s bookings
        public IActionResult MyBookings()
        {
            try
            {
                int? customerId = HttpContext.Session.GetInt32("UserId");

                if (customerId == null)
                {
                    return RedirectToAction("Login");
                }
                var bookings = (from booking in _context.Booking
                                join serviceRequest in _context.ServiceRequests
                                    on booking.Service_Request_Id equals serviceRequest.Request_id
                                join provider in _context.ServiceProviders
                                    on booking.ProviderId equals provider.Provider_id
                                join user in _context.Users
                                    on provider.User_id equals user.User_id
                                join service in _context.Services
                                    on serviceRequest.Service_id equals service.Service_id
                                where serviceRequest.Customer_id == customerId
                                select new
                                {
                                    booking.BookingId,
                                    booking.Status,
                                    booking.Created_At,
                                    ProviderName = user.Name,
                                    ServiceName = service.Service_name
                                }).ToList<dynamic>();
                return View(bookings);
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error fetching bookings: " + ex.Message;
                return View(new List<dynamic>());
            }

           
        }


        // Add Review (5 star)
        [HttpGet]
        public IActionResult AddReview(int bookingId)
        {
            ViewBag.BookingId = bookingId;
            return View();
        }

        [HttpPost]
        public IActionResult AddReview(Reviews review)
        {
            review.Created_At = DateTime.Now;
            _context.Reviews.Add(review);
            _context.SaveChanges();
            TempData["msg"] = "Review submitted!";
            return RedirectToAction("MyBookings");
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Customer");
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
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

                var user = new
                {
                    UserId = Convert.ToInt32(dr["User_id"]),
                    Name = dr["name"].ToString(),
                    Email = dr["email"].ToString(),
                    UserType = dr["UserType"].ToString()
                };

                // Example: Store user info in Session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.Name); // for navbar
                //Session["UserId"] = user.UserId;
                //Session["UserName"] = user.Name;
                //Session["UserType"] = user.UserType;

                _conn.Close();
                return RedirectToAction("MyBookings");  // Redirect to dashboard after successful login
            }
            else
            {
                _conn.Close();
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }
        }

        // Display registration form
        // Show Register form
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Handle Register POST
        [HttpPost]
        public IActionResult Register(users newUser)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate email
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == newUser.Email);
                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "Email is already registered.";
                    return View(newUser);
                }

                newUser.UserType = "Customer";
                newUser.CreatedAt = DateTime.Now;

                _context.Users.Add(newUser);
                _context.SaveChanges();

                // Set session
                HttpContext.Session.SetInt32("UserId", newUser.User_id);
                HttpContext.Session.SetString("UserName", newUser.Name);
                HttpContext.Session.SetString("UserType", newUser.UserType);

                TempData["msg"] = "Registration successful!";
                return RedirectToAction("MakeBooking", "Customer");
            }

            ViewBag.ErrorMessage = "Please correct the errors and try again.";
            return View(newUser);
        }





    }
}
