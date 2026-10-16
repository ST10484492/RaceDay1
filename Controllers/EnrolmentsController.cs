using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using RaceDay1.Models;

namespace RaceDay1.Controllers
{
    [Route("api/enrolments")]
    [ApiController]
    [Authorize(Roles = "Participant")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly connection _connection = new connection();

        // Gets the UserId from the logged-in user's JWT token.
        private bool TryGetUserId(out int userId)
        {
            string? id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(id, out userId);
        }

        // Finds the ParticipantID belonging to the logged-in user.
        private int? GetParticipantId(SqlConnection conn, int userId)
        {
            using var cmd = new SqlCommand(
                @"SELECT ParticipantID
                  FROM dbo.Participants
                  WHERE UserId = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            object? result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }

        // GET: api/enrolments
        // Returns only the logged-in participant's enrolments.
        [HttpGet]
        public IActionResult GetEnrolments()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            using SqlConnection conn = _connection.GetConnection();
            conn.Open();

            int? participantId = GetParticipantId(conn, userId);

            if (participantId == null)
                return NotFound("No participant profile is linked to this account.");

            var enrolments = new List<object>();

            using var cmd = new SqlCommand(
                @"SELECT EnrolmentID, ParticipantID, CategoryID,
                         EnrolmentDate, PaymentStatus, RaceNumber
                  FROM dbo.Enrolments
                  WHERE ParticipantID = @ParticipantID
                  ORDER BY EnrolmentID", conn);

            cmd.Parameters.AddWithValue("@ParticipantID", participantId.Value);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                enrolments.Add(new
                {
                    EnrolmentId = reader.GetInt32(0),
                    ParticipantId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    EnrolmentDate = reader.GetDateTime(3),
                    PaymentStatus = reader.GetString(4),
                    RaceNumber = reader.IsDBNull(5)
                        ? (int?)null
                        : reader.GetInt32(5)
                });
            }

            return Ok(enrolments);
        }

        // GET: api/enrolments/1
        // Returns an enrolment only if it belongs to this participant.
        [HttpGet("{enrolmentId:int}")]
        public IActionResult GetEnrolment(int enrolmentId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            using SqlConnection conn = _connection.GetConnection();
            conn.Open();

            int? participantId = GetParticipantId(conn, userId);

            if (participantId == null)
                return NotFound("Participant profile not found.");

            using var cmd = new SqlCommand(
                @"SELECT EnrolmentID, ParticipantID, CategoryID,
                         EnrolmentDate, PaymentStatus, RaceNumber
                  FROM dbo.Enrolments
                  WHERE EnrolmentID = @EnrolmentID
                    AND ParticipantID = @ParticipantID", conn);

            cmd.Parameters.AddWithValue("@EnrolmentID", enrolmentId);
            cmd.Parameters.AddWithValue("@ParticipantID", participantId.Value);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return NotFound("Enrolment not found.");

            var enrolment = new
            {
                EnrolmentId = reader.GetInt32(0),
                ParticipantId = reader.GetInt32(1),
                CategoryId = reader.GetInt32(2),
                EnrolmentDate = reader.GetDateTime(3),
                PaymentStatus = reader.GetString(4),
                RaceNumber = reader.IsDBNull(5)
                    ? (int?)null
                    : reader.GetInt32(5)
            };

            return Ok(enrolment);
        }

        // POST: api/enrolments
        // The participant ID comes from the token, never from the request.
        [HttpPost]
        public IActionResult CreateEnrolment([FromBody] CreateEnrolmentRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (request.CategoryId <= 0)
                return BadRequest("A valid CategoryId is required.");

            using SqlConnection conn = _connection.GetConnection();
            conn.Open();

            int? participantId = GetParticipantId(conn, userId);

            if (participantId == null)
                return NotFound("Participant profile not found.");

            using var cmd = new SqlCommand(
                @"INSERT INTO dbo.Enrolments (ParticipantID, CategoryID)
                  OUTPUT INSERTED.EnrolmentID
                  VALUES (@ParticipantID, @CategoryID)", conn);

            cmd.Parameters.AddWithValue("@ParticipantID", participantId.Value);
            cmd.Parameters.AddWithValue("@CategoryID", request.CategoryId);

            try
            {
                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                return CreatedAtAction(
                    nameof(GetEnrolment),
                    new { enrolmentId = newId },
                    new
                    {
                        EnrolmentId = newId,
                        ParticipantId = participantId.Value,
                        CategoryId = request.CategoryId
                    });
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return BadRequest("The selected category does not exist.");
            }
        }

        // PUT: api/enrolments/1
        // Participants may change the category of their own enrolment.
        // They cannot change ParticipantID, PaymentStatus or RaceNumber.
        [HttpPut("{enrolmentId:int}")]
        public IActionResult UpdateEnrolment(
            int enrolmentId,
            [FromBody] UpdateEnrolmentRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (request.CategoryId <= 0)
                return BadRequest("A valid CategoryId is required.");

            using SqlConnection conn = _connection.GetConnection();
            conn.Open();

            int? participantId = GetParticipantId(conn, userId);

            if (participantId == null)
                return NotFound("Participant profile not found.");

            using var cmd = new SqlCommand(
                @"UPDATE dbo.Enrolments
                  SET CategoryID = @CategoryID
                  WHERE EnrolmentID = @EnrolmentID
                    AND ParticipantID = @ParticipantID", conn);

            cmd.Parameters.AddWithValue("@CategoryID", request.CategoryId);
            cmd.Parameters.AddWithValue("@EnrolmentID", enrolmentId);
            cmd.Parameters.AddWithValue("@ParticipantID", participantId.Value);

            try
            {
                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound("Enrolment not found.");

                return Ok(new { message = "Enrolment updated successfully." });
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return BadRequest("The selected category does not exist.");
            }
        }

        // DELETE: api/enrolments/1
        // Deletes only an enrolment owned by the logged-in participant.
        [HttpDelete("{enrolmentId:int}")]
        public IActionResult DeleteEnrolment(int enrolmentId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            using SqlConnection conn = _connection.GetConnection();
            conn.Open();

            int? participantId = GetParticipantId(conn, userId);

            if (participantId == null)
                return NotFound("Participant profile not found.");

            using var cmd = new SqlCommand(
                @"DELETE FROM dbo.Enrolments
                  WHERE EnrolmentID = @EnrolmentID
                    AND ParticipantID = @ParticipantID", conn);

            cmd.Parameters.AddWithValue("@EnrolmentID", enrolmentId);
            cmd.Parameters.AddWithValue("@ParticipantID", participantId.Value);

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
                return NotFound("Enrolment not found.");

            return Ok(new { message = "Enrolment deleted successfully." });
        }
    }

    // The request contains only the category.
    // ParticipantID is determined by the API from the JWT token.
    public class CreateEnrolmentRequest
    {
        public int CategoryId { get; set; }
    }

    public class UpdateEnrolmentRequest
    {
        public int CategoryId { get; set; }
    }
}