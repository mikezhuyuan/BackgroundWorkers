using System.Configuration;
using System.Data.SqlClient;

namespace BackgroundWorkers.Demo
{
    static class ConnectionProvider
    {
        public static SqlConnection Connect()
        {
            var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["WebCrawler"].ConnectionString);
            connection.Open();
            return connection;
        }

        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["WebCrawler"].ConnectionString;
            }
        }
    }
}
