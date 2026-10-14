using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1;
using RaceDay1.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganisersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetOrganisers()
        {
            List<organiser> organisers = new List<organiser>();
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Organisers", connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    organisers.Add(new organiser
                    {
                        OrganiserID = Convert.ToInt32(reader["OrganiserID"]),
                        OrganisationName = reader["OrganisationName"].ToString() ?? "",
                        ContactEmail = reader["ContactEmail"].ToString() ?? "",
                        ContactPhone = reader["ContactPhone"].ToString() ?? ""
                    });
                }

                reader.Close();
            }

            return Ok(organisers);
        }
    }
}
