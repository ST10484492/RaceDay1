using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;
using RaceDay1.Services;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace RaceDay1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly PasswordService _passwordService;
        private readonly RaceDayContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            PasswordService passwordService,
            RaceDayContext context,
            IConfiguration configuration)
        {
            _passwordService = passwordService;
            _context = context;
            _configuration = configuration;
        }

        [Authorize(Roles = "Organiser")]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            return Ok("You are authenticated");
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Authentication controller is working");
        }

        public class RegisterRequest
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public DateTime? DateOfBirth { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("First name, last name, email and password are required.");
            }

            string email = request.Email.Trim();

            bool emailExists = await _context.Users
                .AnyAsync(user => user.Email == email);

            if (emailExists)
            {
                return Conflict("An account with this email already exists.");
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                Role = "Participant",
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Registration successful",
                user.UserId,
                user.Email,
                user.Role
            });
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

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            bool passwordIsValid = _passwordService.VerifyPassword(
                user.PasswordHash,
                request.Password);

            if (!passwordIsValid)
            {
                return Unauthorized("Invalid email or password.");
            }

            string token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role,
                Message = "Login successful."
            });
        }

        private string GenerateJwtToken(User user)
        {
            string jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is missing.");

            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is missing.");

            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience is missing.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
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
