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
        private readonly IDataLog _dataLog;
        private readonly ApplicationDbContext _context;
        public CustomerController(ApplicationDbContext context, IConfiguration config, IDataLog dataLog)
        {
            _context = context;
            _config = config;
            _dataLog = dataLog;
        }

        public IActionResult Index()
        {
            var services = _context.Services
                             .Where(s => s.IsActive == true)
                             .ToList();
            ViewBag.services = services;

            var staff = _context.Users
                .Where(u => u.Is_Active == true && u.UserType == "Admin")
                .Select(u => new
                {
                    u.User_id,
                    Name = u.Name ?? "",
                    UserType = u.UserType ?? "",
                    Image = u.Image ?? "default.png"
                })
                .ToList();

            ViewBag.staff = staff;

            var customerReviews = (from review in _context.Reviews
                                 join serviceRequest in _context.ServiceRequests
                                 on review.Service_Request_Id equals serviceRequest.Request_id
                                 join user in _context.Users
                                     on serviceRequest.Customer_id equals user.User_id
                                 where user.Is_Active == true
                                 select new
                                 {
                                     review.Review_id,
                                     review.Rating,
                                     review.Review_Text,
                                     ReviewDate = review.Created_At,

                                     CustomerName = user.Name,
                                     CustomerImage = user.Image ?? "default.png",

                                    
                                 }
                             ).ToList<dynamic>();


            ViewBag.customerReviews = customerReviews;


            return View();
        }



        // Display booking form
        [HttpGet]
        public IActionResult MakeRequest()
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Services = _context.Services.ToList();
            ViewBag.Location = _context.Location.ToList();
            return View();
        }

        public JsonResult GetAreas(string city)
        {
            var data = _context.Location
                         .Where(x => x.City == city)
                         .Select(x => new { x.Location_id, x.Area })
                         .ToList();

            //return Json(data, JsonRequestBehavior.AllowGet);
            return Json(data);
        }

        // Submit booking with time selection
        [HttpPost]
        public IActionResult MakeRequest(ServiceRequest sr)
        {
            try
            {
                int? customerId = HttpContext.Session.GetInt32("UserId");

                if (customerId == null)
                {
                    return RedirectToAction("Login");
                }

                // Set Customer ID
                sr.Customer_id = customerId;

                // Status is already default "Pending"
                sr.Status = "Pending";
                sr.Service_id = sr.Service_id;
                sr.Location_id = sr.Location_id;
    
                // Set Entry date automatically
                sr.Entry_Date = DateTime.Now;

                // Save in database
                _context.ServiceRequests.Add(sr);
                _context.SaveChanges();


                //TempData["msg"] = "Booking Created Successfully!";
                _dataLog.Log("MakeRequest", "one booking request are added suucessfully");
                TempData["ToastMessage"] = "Booking Created Successfully!";
                TempData["ToastType"] = "success"; // success | error | warning | info
                //send mail
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error creating booking: " + ex.Message;
                _dataLog.Log("Customer(MakeRequest)", ex.Message);
                return RedirectToAction("MakeRequest");
            }
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

                var result = (from u in _context.Users
                              join sr in _context.ServiceRequests
                                  on u.User_id equals sr.Customer_id
                              join s in _context.Services
                                  on sr.Service_id equals s.Service_id
                              where u.User_id == customerId
                              select new
                              {
                                  RequestId = sr.Request_id,     // ✅ FIX
                                  Status = sr.Status,            // ✅ FIX
                                  Entry_Date = sr.Entry_Date,
                                  ProviderName = u.Name,
                                  ServiceName = s.Service_name   // ✅ FIX
                              }).ToList<dynamic>();

                return View(result);
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error fetching bookings: " + ex.Message;
                return View(new List<dynamic>());
            }
        }
        public IActionResult CancelBooking(int bookingId)
        {
            var booking = _context.Booking.FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound();

            // Allow cancellation only in Pending or Confirmed status
            if (booking.Status == "Pending" || booking.Status == "Confirmed")
            {
                booking.Status = "Cancelled";
                booking.Modifiy_Date = DateTime.Now;

                _context.SaveChanges();
                TempData["ToastMessage"] = "Booking cancelled successfully!";
                TempData["ToastType"] = "success"; // success | error | warning | info

              
            }
            else
            {
                TempData["ToastMessage"] = "This booking cannot be cancelled.";
                TempData["ToastType"] = "error"; // success | error | warning | info

               
            }

            return RedirectToAction("MyBookings");
        }
        public IActionResult CancelRequest(int requestId)
        {
            try
            {
                // Get logged-in customer id from session
                int? customerId = HttpContext.Session.GetInt32("UserId");

                if (customerId == null)
                {
                    return RedirectToAction("Login");
                }

                // Fetch the booking
                var serviceRequest = _context.ServiceRequests
                    .FirstOrDefault(sr => sr.Request_id == requestId
                                       && sr.Customer_id == customerId);

                if (serviceRequest == null)
                {
                    return NotFound();
                }

                // Update status
                serviceRequest.Status = "Cancelled";
                serviceRequest.Modifiy_Date = DateTime.Now;

                _context.SaveChanges();

                //TempData["Success"] = "Booking cancelled successfully.";
                TempData["ToastMessage"] = "Booking cancelled successfully.";
                TempData["ToastType"] = "success"; // success | error | warning | info
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                _dataLog.Log("Customer(CancelRequest)", ex.Message);
                //TempData["Error"] = "Unable to cancel booking.";
                TempData["ToastMessage"] = "Unable to cancel booking.";
                TempData["ToastType"] = "error"; // success | error | warning | info
                return RedirectToAction("MyBookings");
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
            try
            {
                review.Created_At = DateTime.Now;
                _context.Reviews.Add(review);
                _context.SaveChanges();
                TempData["ToastMessage"] = "Review submitted!";
                TempData["ToastType"] = "success"; // success | error | warning | info
                                                   //TempData["msg"] = "Review submitted!";
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(AddReview)", ex.Message);
                return RedirectToAction("AddReview");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("UserType");

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                if (Request.Cookies["UserId"] != null &&
               Request.Cookies["UserType"] != null)
                {
                    int userId = Convert.ToInt32(Request.Cookies["UserId"]);
                    string userType = Request.Cookies["UserType"];

                    // Restore session
                    HttpContext.Session.SetInt32("UserId", userId);
                    HttpContext.Session.SetString("UserType", userType);

                    if (userType == "Customer")
                        return RedirectToAction("MyBookings");
                    else if (userType == "Admin")
                        return RedirectToAction("Dashboard", "Admin");
                }

                TempData["ToastMessage"] = "Welcome to Login Page!";
                TempData["ToastType"] = "success";

                return View();
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(Login)", ex.Message);
                return RedirectToAction("Login");
            }
            // 🔥 Check cookie first
           
        }

        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            try
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





                    _conn.Close();

                    //if(user.UserType == "Customer") {
                    //    HttpContext.Session.SetInt32("UserId", user.UserId);
                    //    HttpContext.Session.SetString("UserName", user.Name);
                    //    HttpContext.Session.SetString("UserType", user.UserType);
                    //    return RedirectToAction("MyBookings");
                    //}
                    if (user.UserType == "Customer")
                    {
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("UserName", user.Name);
                        HttpContext.Session.SetString("UserType", user.UserType);

                        // 🔥 SAVE COOKIE (30 days)
                        CookieOptions options = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30),
                            HttpOnly = true,
                            Secure = true
                        };

                        Response.Cookies.Append("UserId", user.UserId.ToString(), options);
                        Response.Cookies.Append("UserType", user.UserType, options);

                        return RedirectToAction("MyBookings");
                    }

                    else if (user.UserType == "Admin")
                    {
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("UserName", user.Name);
                        HttpContext.Session.SetString("UserType", user.UserType);
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        TempData["ToastMessage"] = "Your are not Valid user for this site.";
                        TempData["ToastType"] = "   warning"; // success | error | warning | info
                        ViewBag.ErrorMessage = "Your are not Valid user for this site.";
                    }
                    return View();
                    //return RedirectToAction("MyBookings");  // Redirect to dashboard after successful login
                }
                else
                {
                    _conn.Close();
                    ViewBag.ErrorMessage = "Invalid email or password.";
                    return View();
                }
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(Login)", ex.Message);
                return RedirectToAction("Login");
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
        public IActionResult Register(users newUser, IFormFile ImageFile)
        {
            try
            {
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == newUser.Email);
                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "Email already registered.";
                    return View(newUser);
                }

                // ---- SAVE IMAGE -----
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }

                    newUser.Image = fileName;
                }
                else
                {
                    newUser.Image = "default.jpg"; // optional default image
                }

                newUser.CreatedAt = DateTime.Now;

                _context.Users.Add(newUser);
                _context.SaveChanges();

                // set session
                HttpContext.Session.SetInt32("UserId", newUser.User_id);
                HttpContext.Session.SetString("UserName", newUser.Name);
                HttpContext.Session.SetString("UserType", newUser.UserType);
                HttpContext.Session.SetString("UserImage", newUser.Image);

                // redirect based on role
                if (newUser.UserType == "Customer")
                    return RedirectToAction("MakeRequest", "Customer");

                if (newUser.UserType == "Provider")
                    return RedirectToAction("Dashboard", "Provider");

                if (newUser.UserType == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                return RedirectToAction("Login", "Customer");
                //}

                ViewBag.ErrorMessage = "Please correct the errors.";
                return View(newUser);
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(Register)", ex.Message);
                return RedirectToAction("Register");
            }
            
        }
        // GET: Manage Profile
        public IActionResult ManageProfile()
        {
            try
            {
                int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
                var user = _context.Users.FirstOrDefault(x => x.User_id == userId);

                if (user.Image == null)
                {
                    user.Image = "default.png";
                }

                if (user == null)
                    return NotFound();


                return View(user);
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(ManageProfile)", ex.Message);
                return RedirectToAction("ManageProfile");
            }
            
        }

        // POST: Manage Profile Update
        [HttpPost]
        public IActionResult ManageProfile(users model, IFormFile ProfileImage)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(x => x.User_id == model.User_id);

                var emailExists = _context.Users
                           .FirstOrDefault(u => u.Email == model.Email && u.Email != user.Email);
                if (emailExists != null)
                {
                    //ViewBag.ErrorMessage = "Email already registered.";
                    TempData["ToastMessage"] = "Email already registered.";
                    TempData["ToastType"] = "warning"; // success | error | warning | info
                    return View(user);
                }
                if (user == null)
                    return NotFound();

                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ProfileImage.CopyTo(stream);
                    }

                    user.Image = fileName;
                }

                // Update fields
                user.Name = model.Name;
                user.Email = model.Email;
                user.Phone = model.Phone;
                if (model.ConfirmPassword != null)
                {
                    user.Password = model.ConfirmPassword;
                }
                else
                {
                    user.Password = model.Password;
                    TempData["ToastMessage"] = "Profile Updated Successfully!";
                    TempData["ToastType"] = "info"; // success | error | warning | info
                    return View(user);
                }


                _context.SaveChanges();


                TempData["ToastMessage"] = "Profile Updated Successfully!";
                TempData["ToastType"] = "success"; // success | error | warning | info
                return RedirectToAction("ManageProfile");
            }
            catch (Exception ex)
            {

                _dataLog.Log("Customer(ManageProfile)", ex.Message);
                return RedirectToAction("ManageProfile");
            }
          
        }



        // GET: Customer – All Services Display
        public IActionResult AllService()
        {
           // Fetch all active services(you can remove.Where if not needed)
            var services = _context.Services
                              .Where(s => s.IsActive == true)
                              .ToList();
            //return View();
            return View(services);
        }


    }
}
