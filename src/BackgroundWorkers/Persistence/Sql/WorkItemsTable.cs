using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

namespace BackgroundWorkers.Persistence.Sql
{
    public static class WorkItemsTable
    {
        public static void Create(string connectionString)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BackgroundWorkers.Persistence.Sql.WorkItemsTable.sql");
                
            if (stream == null)
                throw new InvalidOperationException("Unable to read WorkItemsTable resource.");

            using (var reader = new StreamReader(stream))
            using(var connection = new SqlConnection(connectionString))
            {
                connection.InfoMessage += connection_InfoMessage;
                var sql = reader.ReadToEnd();
                
                connection.Open();
                var command =  connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        static void connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}