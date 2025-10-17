using System.Data;
using LocalLinker.App_Data;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
    }
}
