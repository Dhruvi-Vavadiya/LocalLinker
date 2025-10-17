using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using LocalLinker.App_Data;
using LocalLinker.Models;

namespace LocalLinker.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        MySqlConnection _conn;
        MySqlCommand _cmd = new MySqlCommand();
        Include _inc = new Include();

        private readonly ApplicationDbContext _context;

        public AdminController(ILogger<HomeController> logger, IConfiguration config, ApplicationDbContext context)
        {
            _logger = logger;
            _config = config;
            _context = context;
        }
        public IActionResult Dashboard()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalProviders = _context.Users.Count(u => u.UserType == "Provider");
            ViewBag.TotalBookings = _context.Booking.Count();
            ViewBag.PendingBookings = _context.Booking.Count(b => b.Status == "Pending");
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Dashboard");
        }

        // List All Bookings
        public IActionResult Bookings()
        {
            var bookings = (from b in _context.Booking
                            join sp in _context.ServiceProviders on b.ProviderId equals sp.Provider_id
                            join u in _context.Users on sp.User_id equals u.User_id
                            join sr in _context.ServiceRequests on b.Service_Request_Id equals sr.Request_id
                            join s in _context.Services on sr.Service_id equals s.Service_id
                            select new
                            {
                                b.BookingId,
                                b.Status,
                                b.Created_At,
                                ProviderName = u.Name,
                                ServiceName = s.Service_name
                            }).ToList<dynamic>();

            return View(bookings);
        }


        // Reports - generate simple booking count report
        public IActionResult Reports()
        {
            var report = _context.Booking
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            return View(report);
        }
        // =======================
        // MANAGE SERVICES SECTION
        // =======================

        // List all services
        public IActionResult Services()
        {
            var services = _context.Services.ToList();
            return View(services);
        }

        // GET: Create Service
        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        // POST: Create Service
        [HttpPost]
        public IActionResult CreateService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                _context.SaveChanges();
                return RedirectToAction("Services");
            }
            return View(service);
        }

        // GET: Edit Service
        [HttpGet]
        public IActionResult EditService(int id)
        {
            var service = _context.Services.Find(id);
            if (service == null) return NotFound();

            return View(service);
        }

        // POST: Edit Service
        [HttpPost]
        public IActionResult EditService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Update(service);
                _context.SaveChanges();
                return RedirectToAction("Services");
            }
            return View(service);
        }

        // GET: Delete Service
        public IActionResult DeleteService(int id)
        {
            var service = _context.Services.Find(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                _context.SaveChanges();
            }
            return RedirectToAction("Services");
        }

        // GET: AdminController
        public ActionResult Index()
        {
            _conn = _inc.db_locallinker(_config);
            _cmd.Connection = _conn;
            _conn.Open();
            _cmd.CommandText = "SELECT * FROM users WHERE UserType='Provider'";
            MySqlDataReader dr = _cmd.ExecuteReader();
            List<LocalLinker.Models.users> lst = new List<LocalLinker.Models.users>();
            while (dr.Read())
            {
                var user = new LocalLinker.Models.users
                {
                    User_id = Convert.ToInt32(dr["User_id"]),
                    Name = dr["name"].ToString(),
                    Email = dr["email"].ToString(),
                    Phone = dr["phone"].ToString(),
                    UserType = dr["UserType"].ToString()
                };
                lst.Add(user);
            }

            return View(lst);
        }


    }
}
