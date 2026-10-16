using Microsoft.Data.SqlClient;
using System;

    namespace RaceDay1
{
    public class connection
    {

        string connectionString = @"Data source=(localdb)\api_both;database=RaceDayDB;Initial catalog=RaceDayDB;Integrated Security=True;TrustServerCertificate=True";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
