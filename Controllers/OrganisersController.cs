using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1;
using RaceDay1.Models;
using Microsoft.AspNetCore.Authorization;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Organiser")]
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
                        OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                        OrganisationName = reader["OrganisationName"].ToString() ?? "",
                        ContactEmail = reader["ContactEmail"].ToString() ?? "",
                        ContactPhone = reader["ContactPhone"].ToString() ?? ""
                    });
                }

                reader.Close();
            }

            return Ok(organisers);
        }

        [HttpPost]
        public IActionResult AddOrganisers(organiser organiser)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"INSERT INTO Organisers (OrganisationName, ContactEmail, ContactPhone)
                               VALUES (@OrganisationName, @ContactEmail, @ContactPhone)";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@OrganisationName", organiser.OrganisationName);
                command.Parameters.AddWithValue("@ContactEmail", organiser.ContactEmail);
                command.Parameters.AddWithValue("@ContactPhone", organiser.ContactPhone);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Ok("Organiser Successfully Added");
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
