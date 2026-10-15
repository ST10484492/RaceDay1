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
                        EventId = Convert.ToInt32(reader["EventID"]),
                        OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
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

        [HttpPut("{id}")]
        public IActionResult UpdateOrganisers(int id, organiser organiser)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"UPDATE Organisers SET OrganisationName = @OrganisationName,
                               ContactEmail = @ContactEmail, ContactPhone = @ContactPhone
                               WHERE OrganiserID = @OrganiserID";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("OrganiserID", id);
                command.Parameters.AddWithValue("@OrganisationName", organiser.OrganisationName);
                command.Parameters.AddWithValue("@ContactEmail", organiser.ContactEmail);
                command.Parameters.AddWithValue("@ContactPhone", organiser.ContactPhone);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound("Organiser not found");
                }
            }

            return Ok("Organiser updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteOrganisers(int id)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "DELETE FROM Organisers WHERE OrganiserID = @OrganiserID";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@OrganiserID", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound("Organiser not found");
                }

            }

            return Ok("Organiser deleted successfully");
        }
    }
}
