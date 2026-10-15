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
                        ParticipantId = Convert.ToInt32(reader["ParticipantID"]),
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

        [HttpPost]
        public IActionResult AddParticipant(participant participant)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"INSERT INTO Participants (FirstName, LastName, Email, DateOfBirth, Gender, RegistrationDate)
                               VALUES (@FirstName, @LastName, @Email, @DateOfBirth, @Gender, @RegistrationsDate)";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@FirstName", participant.FirstName);
                command.Parameters.AddWithValue("@LastName", participant.LastName);
                command.Parameters.AddWithValue("@Email", participant.Email);
                command.Parameters.AddWithValue("@DateOfBirth", participant.DateOfBirth);
                command.Parameters.AddWithValue("@Gender", participant.Gender);
                command.Parameters.AddWithValue("@RegistrationsDate", DateTime.Now);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Ok("Participant Successfully Registered");
        }

        [HttpPut("{id}")]
        public IActionResult UpdateParticipant(int id, participant participant)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"UPDATE Participants SET FirstName = @FirstName,
                               LastName = @LastName, Email = @Email, DateOfBirth = @DateOfBirth,
                               Gender = @Gender WHERE ParticipantID = @ParticipantID";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@ParticipantID", id);
                command.Parameters.AddWithValue("@FirstName", participant.FirstName);
                command.Parameters.AddWithValue("@LastName", participant.LastName);
                command.Parameters.AddWithValue("@Email", participant.Email);
                command.Parameters.AddWithValue("@DateOfBirth", participant.DateOfBirth);
                command.Parameters.AddWithValue("@Gender", participant.Gender);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound("PArticipant not found");
                }
            }

            return Ok("Participant updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteParticipant (int id)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "DELETE FROM Participants WHERE ParticipantID = @ParticipantID";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ParticipantID", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound("Participant not found");
                }

            }

            return Ok("Particpant deleted successfully");
        }
    }
}
