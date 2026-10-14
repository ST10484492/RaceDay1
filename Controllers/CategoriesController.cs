using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCategories()
        {
            List<categories> categories = new List<categories>();

            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "SELECT * FROM Categories";

                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    categories category = new categories
                    {
                        CategoryID = Convert.ToInt32(reader["CategoryID"]),
                        EventID = Convert.ToInt32(reader["EventID"]),
                        CategoryName = reader["CategoryName"].ToString() ?? "",
                        DistanceKM = Convert.ToDecimal(reader["DistanceKM"]),
                        EntryFee = Convert.ToDecimal(reader["EntryFee"])
                    };

                    categories.Add(category);
                }

                reader.Close();
            }

            return Ok(categories);
        }
    }
}
