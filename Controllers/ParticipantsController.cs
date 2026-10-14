using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipantsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetParticipants()
        {
            List<participant> participants = new List<participant>();

            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "SELECT * FROM Participants";

                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    participant participant = new participant
                    {
                        ParticipantID = Convert.ToInt32(reader["ParticipantID"]),
                        FirstName = reader["FirstName"].ToString() ?? "",
                        LastName = reader["LastName"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? "",
                        DateOfBirth = Convert.ToDateTime(reader["DateOfBirth"]),
                        Gender = reader["Gender"].ToString() ?? "",
                        RegistrationsDate = Convert.ToDateTime(reader["RegistrationDate"])
                    };

                    participants.Add(participant);
                }

                reader.Close();
            }

            return Ok(participants);
        }
    }
}
