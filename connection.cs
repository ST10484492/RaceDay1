using Microsoft.Data.SqlClient;

    namespace RaceDay1
{
    public class connection
    {

        string connectionString = @"Data source=(localdb)\api_both;database=RaceDayDB;";

        public SqlConnection GetSqlConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
