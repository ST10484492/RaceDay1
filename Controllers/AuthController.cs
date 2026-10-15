using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay1.Models;
using RaceDay1.Services;
using System;
using System.Linq;
using System.Security.Policy;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace RaceDay1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly PasswordService _passwordService;
        private readonly UserStore _userStore;

        public AuthController(PasswordService passwordService, UserStore userStore)
        {
            _passwordService = passwordService;
            _userStore = userStore;
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
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("All fields are required");
            }

            bool emailExists = _userStore.Users.Any(user => user.Email.Equals(request.Email.Trim(),
                StringComparison.OrdinalIgnoreCase));

            if (emailExists)
            {
                return Conflict("An account with this email already exists");
            }

            var user = new User
            {

                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = _passwordService.HashPassword(request.Password),
                Role = "Participant"
            };

            _userStore.Users.Add(user);

            return Ok(new
            {
                Message = "Registration successful",
                user.Email,
                user.Role
            });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var user = _userStore.Users.FirstOrDefault(
                u => u.Email.Equals(
                    request.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase));

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
            var key = Encoding.UTF8.GetBytes(
                "RaceDay1-Development-Key-Change-This-123456789");

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "RaceDay1",
                audience: "RaceDay1Users",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
