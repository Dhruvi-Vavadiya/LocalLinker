using System.Diagnostics;
using LocalLinker.App_Data;
using LocalLinker.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;


namespace LocalLinker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        MySqlConnection _conn;
        MySqlCommand _cmd = new MySqlCommand();
        Include _inc =new Include();

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
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
                //Session["UserId"] = user.UserId;
                //Session["UserName"] = user.Name;
                //Session["UserType"] = user.UserType;

                _conn.Close();
                return RedirectToAction("Privacy", "Home");  // Redirect to dashboard after successful login
            }
            else
            {
                _conn.Close();
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
