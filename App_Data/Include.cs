
using MySql.Data.MySqlClient;

namespace LocalLinker.App_Data
{
    public class Include
    {
        public MySqlConnection db_locallinker(IConfiguration config)
        {
            MySqlConnection conn = new MySqlConnection(config.GetConnectionString("dbcs"));
            return conn;
        }
    }
}
