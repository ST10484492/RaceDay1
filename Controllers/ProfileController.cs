using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace RaceDay1.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProfileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Use the same database connection configured in appsettings.json.
        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from appsettings.json.");

        // Get the ID of the account from the user's JWT token.
        private bool TryGetUserId(out int userId)
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out userId);
        }

        // Return the logged-in user's account and participant details.
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid user token.");

            const string sql = @"
                SELECT
                    u.UserId,
                    u.FirstName,
                    p.LastName,
                    u.Email,
                    u.Role,
                    u.DateOfBirth,
                    p.Gender
                FROM dbo.Users AS u
                LEFT JOIN dbo.Participants AS p
                    ON p.UserId = u.UserId
                WHERE u.UserId = @UserId;";

            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound("User account not found.");

            // Return only the information needed for the profile.
            return Ok(new
            {
                UserId = reader["UserId"],
                FirstName = reader["FirstName"],
                LastName = reader["LastName"] == DBNull.Value
                    ? null : reader["LastName"],
                Email = reader["Email"],
                Role = reader["Role"] == DBNull.Value
                    ? "Participant" : reader["Role"],
                DateOfBirth = reader["DateOfBirth"] == DBNull.Value
                    ? null : reader["DateOfBirth"],
                Gender = reader["Gender"] == DBNull.Value
                    ? null : reader["Gender"]
            });
        }

        // Fields a user is allowed to update in their own profile.
        public class UpdateProfileRequest
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime? DateOfBirth { get; set; }
            public string? Gender { get; set; }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid user token.");

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                request.DateOfBirth == null)
            {
                return BadRequest(
                    "First name, last name, email and date of birth are required.");
            }

            if (request.DateOfBirth.Value.Date >= DateTime.Today)
                return BadRequest("Date of birth must be earlier than today.");

            string email = request.Email.Trim();

            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // Prevent the email from being assigned to another account.
                const string duplicateSql = @"
                    SELECT COUNT(*)
                    FROM dbo.Users
                    WHERE Email = @Email AND UserId <> @UserId;";

                using (var duplicateCommand = new SqlCommand(
                    duplicateSql, connection, transaction))
                {
                    duplicateCommand.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 256).Value = email;

                    duplicateCommand.Parameters.Add(
                        "@UserId", SqlDbType.Int).Value = userId;

                    int duplicates = Convert.ToInt32(
                        await duplicateCommand.ExecuteScalarAsync());

                    if (duplicates > 0)
                    {
                        transaction.Rollback();
                        return Conflict("That email is already in use.");
                    }
                }

                // Update the login account.
                const string updateUserSql = @"
                    UPDATE dbo.Users
                    SET FirstName = @FirstName,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth
                    WHERE UserId = @UserId;";

                using (var userCommand = new SqlCommand(
                    updateUserSql, connection, transaction))
                {
                    userCommand.Parameters.Add(
                        "@FirstName", SqlDbType.NVarChar, 100).Value =
                        request.FirstName.Trim();

                    userCommand.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 256).Value = email;

                    userCommand.Parameters.Add(
                        "@DateOfBirth", SqlDbType.Date).Value =
                        request.DateOfBirth.Value.Date;

                    userCommand.Parameters.Add(
                        "@UserId", SqlDbType.Int).Value = userId;

                    int rowsUpdated = await userCommand.ExecuteNonQueryAsync();

                    if (rowsUpdated == 0)
                    {
                        transaction.Rollback();
                        return NotFound("User account not found.");
                    }
                }

                // Update the matching race participant, if one exists.
                const string updateParticipantSql = @"
                    UPDATE dbo.Participants
                    SET FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender
                    WHERE UserId = @UserId;";

                using (var participantCommand = new SqlCommand(
                    updateParticipantSql, connection, transaction))
                {
                    participantCommand.Parameters.Add(
                        "@FirstName", SqlDbType.VarChar, 100).Value =
                        request.FirstName.Trim();

                    participantCommand.Parameters.Add(
                        "@LastName", SqlDbType.VarChar, 100).Value =
                        request.LastName.Trim();

                    participantCommand.Parameters.Add(
                        "@Email", SqlDbType.VarChar, 256).Value = email;

                    participantCommand.Parameters.Add(
                        "@DateOfBirth", SqlDbType.Date).Value =
                        request.DateOfBirth.Value.Date;

                    participantCommand.Parameters.Add(
                        "@Gender", SqlDbType.VarChar, 50).Value =
                        string.IsNullOrWhiteSpace(request.Gender)
                            ? DBNull.Value
                            : request.Gender.Trim();

                    participantCommand.Parameters.Add(
                        "@UserId", SqlDbType.Int).Value = userId;

                    await participantCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();

                return Ok(new { Message = "Profile updated successfully." });
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // The transaction may already have been rolled back.
                }

                throw;
            }
        }
    }
}