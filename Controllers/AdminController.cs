using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using LocalLinker.App_Data;
using LocalLinker.Models;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
//using ClosedXML.Excel;

using System.IO;

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
            if (HttpContext.Session.GetString("UserType") == "Admin")
            {
                ViewBag.TotalUsers = _context.Users.Count();
                ViewBag.TotalProviders = _context.Users.Count(u => u.UserType == "Provider");
                ViewBag.TotalBookings = _context.Booking.Count();
                ViewBag.PendingBookings = _context.Booking.Count(b => b.Status == "Pending");

                var report = (from sp in _context.ServiceProviders
                              join u in _context.Users
                                  on sp.User_id equals u.User_id
                              join b in _context.Booking
                                  on sp.Provider_id equals b.ProviderId into bookingGroup
                              select new
                              {
                                  ProviderId = sp.Provider_id,
                                  ProviderName = u.Name,

                                  TotalBookings = bookingGroup.Count(),
                                  CompletedBookings = bookingGroup.Count(x => x.Status == "Completed"),
                                  PendingBookings = bookingGroup.Count(x => x.Status != "Completed"),

                                  TotalEarnings = bookingGroup
                                      .Where(x => x.Status == "Completed")
                                      .Sum(x => (decimal?)x.Amount) ?? 0
                              }).ToList();

                // Pie chart data
                ViewBag.ProviderNames = report.Select(x => x.ProviderName).ToList();
                ViewBag.CompletedBookings = report.Select(x => x.CompletedBookings).ToList();
                ViewBag.PendingBookingss = report.Select(x => x.PendingBookings).ToList();
                ViewBag.Earnings = report.Select(x => x.TotalEarnings).ToList();
                return View();
            }
            else
            {
                TempData["ToastMessage"] = "First you login with valid usertype Admin";
                TempData["ToastType"] = "warning"; // success | error | warning | info
                return RedirectToAction("Login", "Customer");
                //return View();
            }

        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Customer");
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

        public IActionResult DeactivateService(int id)
        {
            var service = _context.Services.Find(id);
            if (service == null)
                return NotFound();

            service.IsActive = false;
            _context.SaveChanges();

            return RedirectToAction("Services");
        }

        public IActionResult ActivateService(int id)
        {
            var service = _context.Services.Find(id);
            if (service == null)
                return NotFound();

            service.IsActive = true;
            _context.SaveChanges();

            return RedirectToAction("Services");
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
        //public IActionResult DeleteService(int id)
        //{
        //    var service = _context.Services.Find(id);
        //    if (service != null)
        //    {
        //        _context.Services.Remove(service);
        //        _context.SaveChanges();
        //    }
        //    return RedirectToAction("Services");
        //}

        // All suer
        public ActionResult Index()
        {
            _conn = _inc.db_locallinker(_config);
            _cmd.Connection = _conn;
            _conn.Open();
            _cmd.CommandText = "SELECT * FROM users WHERE UserType !='Admin'";
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
        [HttpPost]
        public IActionResult UpdateStatus(int id, bool isActive)
        {
            try
            {
                _conn = _inc.db_locallinker(_config);
                _cmd.Connection = _conn;
                _conn.Open();

                _cmd.CommandText = "UPDATE users SET Is_Active=@active WHERE User_id=@id";
                _cmd.Parameters.Clear();
                _cmd.Parameters.AddWithValue("@active", isActive);
                _cmd.Parameters.AddWithValue("@id", id);

                _cmd.ExecuteNonQuery();
                _conn.Close();

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }


        public IActionResult Servicesrequest()
        {
            var requests = (from sr in _context.ServiceRequests
                            join s in _context.Services
                                on sr.Service_id equals s.Service_id into serviceJoin
                            from s in serviceJoin.DefaultIfEmpty()

                            join l in _context.Location
                                on sr.Location_id equals l.Location_id into locationJoin
                            from l in locationJoin.DefaultIfEmpty()

                            join u in _context.Users
                                on sr.Customer_id equals u.User_id into userJoin
                            from u in userJoin.DefaultIfEmpty()

                            select new
                            {
                                RequestId = sr.Request_id,
                                CustomerName = u.Name,
                                ServiceName = s.Service_name,
                                LocationName = l.City + " " + l.Area,
                                sr.Description,
                                sr.Status,
                                sr.Entry_Date
                            }).ToList<dynamic>();

            return View(requests);
        }

        public IActionResult ServicesProvider()
        {
            var providers = (from sp in _context.ServiceProviders
                             join u in _context.Users
                                 on sp.User_id equals u.User_id
                             join s in _context.Services
                                 on sp.Service_id equals s.Service_id
                             join l in _context.Location
                                 on sp.Location_id equals l.Location_id
                             where u.UserType == "Provider"
                             select new
                             {
                                 ProviderId = sp.Provider_id,
                                 ProviderName = u.Name,
                                 ServiceName = s.Service_name,
                                 LocationName = l.City + " " + l.Area,
                                 sp.Experience_years,
                                 sp.Description,
                                 sp.IsVerified
                             }).ToList<dynamic>();

            return View(providers);
        }
        public IActionResult ActivateProvider(int id)
        {
            var provider = _context.ServiceProviders
                                   .FirstOrDefault(x => x.Provider_id == id);

            if (provider == null)
                return NotFound();

            provider.IsVerified = true;
            _context.SaveChanges();

            TempData["Success"] = "Provider activated successfully";
            return RedirectToAction("ServicesProvider");
        }

        public IActionResult DeactivateProvider(int id)
        {
            var provider = _context.ServiceProviders
                                   .FirstOrDefault(x => x.Provider_id == id);

            if (provider == null)
                return NotFound();

            provider.IsVerified = false;
            _context.SaveChanges();

            TempData["Success"] = "Provider deactivated successfully";
            return RedirectToAction("ServicesProvider");
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
                TempData["Error"] = "Unable to load reviews.";
                return View(new List<dynamic>());
            }
        }

        public IActionResult ServiceReport()
        {
            try
            {
                var report = (from b in _context.Booking
                              join sr in _context.ServiceRequests
                                  on b.Service_Request_Id equals sr.Request_id
                              join s in _context.Services
                                  on sr.Service_id equals s.Service_id
                              join r in _context.Reviews
                                  on sr.Request_id equals r.Service_Request_Id into reviewJoin
                              from r in reviewJoin.DefaultIfEmpty()
                              group r by s.Service_name into g
                              select new
                              {
                                  ServiceName = g.Key,
                                  TotalBookings = g.Count(),
                                  AvgRating = g.Average(x => (double?)x.Rating) ?? 0
                              }).ToList<dynamic>();

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load report.";
                return View(new List<dynamic>());
            }
        }

        public IActionResult ProviderPerformance()
        {
            var report = (from sp in _context.ServiceProviders
                          join u in _context.Users
                              on sp.User_id equals u.User_id
                          join b in _context.Booking
                              on sp.Provider_id equals b.ProviderId into bookingGroup
                          select new
                          {
                              ProviderId = sp.Provider_id,
                              ProviderName = u.Name,
                              TotalBookings = bookingGroup.Count(),
                              CompletedBookings = bookingGroup.Count(x => x.Status == "Completed"),
                              TotalEarnings = bookingGroup
                                  .Where(x => x.Status == "Completed")
                                  .Sum(x => (decimal?)x.Amount) ?? 0,
                              AverageRating = (
                                  from r in _context.Reviews
                                  join sr in _context.ServiceRequests
                                      on r.Service_Request_Id equals sr.Request_id
                                  where bookingGroup.Select(b => b.Service_Request_Id)
                                      .Contains(r.Service_Request_Id)
                                  select (double?)r.Rating
                              ).Average() ?? 0
                          }).ToList();

            ViewBag.Report = report;
            return View();
        }


        //public IActionResult ExportProviderPerformancePdf()
        //{
        //    var data = (from sp in _context.ServiceProviders
        //                join u in _context.Users
        //                    on sp.User_id equals u.User_id
        //                select new
        //                {
        //                    ProviderName = u.Name,
        //                    TotalBookings = _context.Booking.Count(b => b.ProviderId == sp.Provider_id),
        //                    CompletedBookings = _context.Booking.Count(b =>
        //                        b.ProviderId == sp.Provider_id && b.Status == "Completed")
        //                }).ToList();

        //    using var stream = new MemoryStream();
        //    var writer = new PdfWriter(stream);
        //    var pdf = new PdfDocument(writer);
        //    var doc = new Document(pdf);

        //    doc.Add(new Paragraph("Provider Performance Report\n\n"));

        //    var table = new Table(3);
        //    table.AddHeaderCell("Provider");
        //    table.AddHeaderCell("Total Bookings");
        //    table.AddHeaderCell("Completed");

        //    foreach (var item in data)
        //    {
        //        table.AddCell(item.ProviderName);
        //        table.AddCell(item.TotalBookings.ToString());
        //        table.AddCell(item.CompletedBookings.ToString());
        //    }

        //    doc.Add(table);
        //    doc.Close();

        //    return File(stream.ToArray(), "application/pdf", "ProviderPerformance.pdf");
        //}
        public IActionResult ExportProviderPerformanceExcel()
        {
            var data = (from sp in _context.ServiceProviders
                        join u in _context.Users
                            on sp.User_id equals u.User_id
                        select new
                        {
                            ProviderName = u.Name,
                            TotalBookings = _context.Booking.Count(b => b.ProviderId == sp.Provider_id),
                            CompletedBookings = _context.Booking.Count(b =>
                                b.ProviderId == sp.Provider_id && b.Status == "Completed")
                        }).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Provider Report");

            ws.Cell(1, 1).Value = "Provider";
            ws.Cell(1, 2).Value = "Total Bookings";
            ws.Cell(1, 3).Value = "Completed";

            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.ProviderName;
                ws.Cell(row, 2).Value = item.TotalBookings;
                ws.Cell(row, 3).Value = item.CompletedBookings;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProviderPerformance.xlsx"
            );
        }
        public IActionResult Locations()
        {
            var locations = _context.Location.ToList();
            return View(locations);
        }
        public IActionResult AddLocation()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddLocation(Location location)
        {
            if (ModelState.IsValid)
            {
                _context.Location.Add(location);
                _context.SaveChanges();
                return RedirectToAction("Locations");
            }
            return View(location);
        }
        public IActionResult EditLocation(int id)
        {
            var location = _context.Location.Find(id);
            if (location == null)
                return NotFound();

            return View(location);
        }
        [HttpPost]
        public IActionResult EditLocation(Location location)
        {
            if (ModelState.IsValid)
            {
                _context.Location.Update(location);
                _context.SaveChanges();
                return RedirectToAction("Locations");
            }
            return View(location);
        }
        public IActionResult DeleteLocation(int id)
        {
            var location = _context.Location.Find(id);
            if (location == null)
                return NotFound();

            _context.Location.Remove(location);
            _context.SaveChanges();
            return RedirectToAction("Locations");
        }


        //===========================
    }
}
