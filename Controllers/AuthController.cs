using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RaceDay1.Models;
using RaceDay1.Services;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDay1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthController(
            PasswordService passwordService,
            IConfiguration configuration)
        {
            _passwordService = passwordService;
            _configuration = configuration;
        }

        // Read the same database connection string used by Program.cs.
        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from appsettings.json.");

        // Simple public endpoint to check that the controller is running.
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Authentication controller is working.");
        }

        // Only users with the Organiser role can access this endpoint.
        [Authorize(Roles = "Organiser")]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            return Ok("You are authenticated as an organiser.");
        }

        // Information required when someone registers as a participant.
        public class RegisterRequest
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public DateTime? DateOfBirth { get; set; }
            public string? Gender { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            // Validate the required registration details first.
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                request.DateOfBirth == null)
            {
                return BadRequest(
                    "First name, last name, email, password and date of birth are required.");
            }

            if (request.DateOfBirth.Value.Date >= DateTime.Today)
            {
                return BadRequest(
                    "Date of birth must be earlier than today.");
            }

            string email = request.Email.Trim();

            // Hash the password before storing it.
            string passwordHash =
                _passwordService.HashPassword(request.Password);

            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            // Both database inserts must succeed, or neither is saved.
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // Check that the email is not already used by an account
                // or a separately created participant record.
                const string checkEmailSql = @"
                    SELECT
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.Users WHERE Email = @Email
                        ) OR EXISTS (
                            SELECT 1 FROM dbo.Participants WHERE Email = @Email
                        )
                        THEN 1 ELSE 0 END;";

                using (var checkCommand = new SqlCommand(
                    checkEmailSql, connection, transaction))
                {
                    checkCommand.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 256).Value = email;

                    int emailExists = Convert.ToInt32(
                        await checkCommand.ExecuteScalarAsync());

                    if (emailExists == 1)
                    {
                        transaction.Rollback();
                        return Conflict(
                            "An account or participant with this email already exists.");
                    }
                }

                // 1. Create the login account in dbo.Users.
                // UserId is generated automatically by SQL Server.
                const string insertUserSql = @"
                    INSERT INTO dbo.Users
                        (FirstName, Email, PasswordHash, Role, DateOfBirth)
                    OUTPUT INSERTED.UserId
                    VALUES
                        (@FirstName, @Email, @PasswordHash,
                         @Role, @DateOfBirth);";

                int userId;

                using (var userCommand = new SqlCommand(
                    insertUserSql, connection, transaction))
                {
                    userCommand.Parameters.Add(
                        "@FirstName", SqlDbType.NVarChar, 100).Value =
                        request.FirstName.Trim();

                    userCommand.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 256).Value = email;

                    userCommand.Parameters.Add(
                        "@PasswordHash", SqlDbType.NVarChar, -1).Value =
                        passwordHash;

                    // Public registration can only create Participant accounts.
                    userCommand.Parameters.Add(
                        "@Role", SqlDbType.NVarChar, 50).Value = "Participant";

                    userCommand.Parameters.Add(
                        "@DateOfBirth", SqlDbType.Date).Value =
                        request.DateOfBirth.Value.Date;

                    userId = Convert.ToInt32(
                        await userCommand.ExecuteScalarAsync());
                }

                // 2. Create the race participant and link it to the account.
                const string insertParticipantSql = @"
                    INSERT INTO dbo.Participants
                        (UserId, FirstName, LastName, Email,
                         DateOfBirth, Gender)
                    VALUES
                        (@UserId, @FirstName, @LastName, @Email,
                         @DateOfBirth, @Gender);";

                using (var participantCommand = new SqlCommand(
                    insertParticipantSql, connection, transaction))
                {
                    participantCommand.Parameters.Add(
                        "@UserId", SqlDbType.Int).Value = userId;

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

                    // RegistrationDate uses its SQL DEFAULT (GETDATE()).
                    await participantCommand.ExecuteNonQueryAsync();
                }

                // Save both inserts together only after both have succeeded.
                transaction.Commit();

                return Created("/api/Auth/login", new
                {
                    Message = "Registration successful.",
                    UserId = userId,
                    Email = email,
                    Role = "Participant"
                });
            }
            catch
            {
                // Undo either insert if any database operation fails.
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // The transaction may already have been rolled back.
                }

                // Let ASP.NET Core handle and log the database exception.
                throw;
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            string email = request.Email.Trim();

            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            // Retrieve the account using its email address.
            const string loginSql = @"
                SELECT UserId, Email, PasswordHash, Role
                FROM dbo.Users
                WHERE Email = @Email;";

            int userId;
            string storedEmail;
            string passwordHash;
            string role;

            using (var command = new SqlCommand(loginSql, connection))
            {
                command.Parameters.Add(
                    "@Email", SqlDbType.NVarChar, 256).Value = email;

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Unauthorized("Invalid email or password.");
                }

                userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                storedEmail = reader.GetString(reader.GetOrdinal("Email"));
                passwordHash = reader.GetString(
                    reader.GetOrdinal("PasswordHash"));

                // Treat a missing role as Participant.
                int roleIndex = reader.GetOrdinal("Role");
                role = reader.IsDBNull(roleIndex)
                    ? "Participant"
                    : reader.GetString(roleIndex);
            }

            // Compare the submitted password with the stored hash.
            if (!_passwordService.VerifyPassword(
                    passwordHash, request.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            // Create a signed JWT token for the authenticated account.
            string token = GenerateJwtToken(userId, storedEmail, role);

            return Ok(new LoginResponse
            {
                Token = token,
                Email = storedEmail,
                Role = role,
                Message = "Login successful."
            });
        }

        private string GenerateJwtToken(
            int userId, string email, string role)
        {
            string jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is missing.");

            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is missing.");

            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience is missing.");

            // These claims identify the user and determine their permissions.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
