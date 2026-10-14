using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetEvents()
        {
            List<RaceDay1.Models.@event> events = new List<RaceDay1.Models.@event>();
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Events", connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    events.Add(new RaceDay1.Models.@event
                    {
                        EventID = Convert.ToInt32(reader["EventID"]),
                        OrganiserID = Convert.ToInt32(reader["OrganiserID"]),
                        EventName = reader["EventName"].ToString() ?? "",
                        EventDate = Convert.ToDateTime(reader["EventDate"]),
                        Location = reader["Location"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? ""
                    });
                }

                reader.Close();
            }

            return Ok(events);
        }
    }
}
