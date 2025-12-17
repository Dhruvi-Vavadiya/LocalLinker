using LocalLinker.App_Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net;
using System.Net.Mail;
//using static Org.BouncyCastle.Math.EC.ECCurve;

namespace LocalLinker.Models
{
    public class ErrorLog : IDataLog
    {
        private readonly IConfiguration _configuration;
        private MySqlConnection _connection;
        //private SqlCommand _command;
        MySqlCommand _command = new MySqlCommand();
        Include _inc = new Include();

        public ErrorLog(IConfiguration config)
        {
            _configuration = config;
           
           
         
        }
        public void Log(string methodname ,string message)
        {
            _command.Parameters.Clear();
           string  mess = methodname + " : " + message;
            _connection = _inc.db_locallinker(_configuration);
            _command.Connection = _connection;
            _connection.Open();
            _command.CommandType = System.Data.CommandType.Text;

            //_command.CommandText = "insert into TblError values ('" + message + "')";
            _command.CommandText = "INSERT INTO TblError (Message, LogDate) VALUES (@Message, @LogDate)";
            _command.Parameters.AddWithValue("@Message", mess);
            _command.Parameters.AddWithValue("@LogDate", DateTime.Now);
            //_command.Parameters.Add("@Message", SqlDbType.VarChar).Value = mess;
            //_command.Parameters.Add("@LogDate", SqlDbType.DateTime).Value = DateTime.Now;
            _command.ExecuteNonQuery();
            _connection.Close();

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

                        Log("Provider(ErrorLog(SendEmail))", "mail is sended from " + toEmail);
                    }
                }
            }
            catch (SmtpException ex)
            {
                // Log this
                Log("Provider(ErrorLog(SendEmail))", ex.Message);
                Console.WriteLine(ex.Message);
            }

        }
        //void IDataLog.ShowNotification(string message, string type)
        //{
        //    HttpContext.Session.SetInt32("UserId", message);
        //    TempData["dfcdfv"] = "sdfdsf";
        //    TempData["Notification"] = message;
        //    TempData["NotificationType"] = type;
        //    throw new NotImplementedException();
        //}
    }
}
