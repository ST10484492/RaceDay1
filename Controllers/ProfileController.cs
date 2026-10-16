using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RaceDay1.Models;
using RaceDay1.Services;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserStore _userStore;

        public ProfileController(UserStore userStore)
        {
            _userStore = userStore;
        }

        // GET: api/profile
        [HttpGet]
        public IActionResult GetProfile()
        {
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("User identity was not found.");
            }

            User? user = _userStore.Users
                .FirstOrDefault(u =>
                    u.Email.Equals(email,
                        StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return NotFound("Profile not found.");
            }

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.Phone,
                user.DateOfBirth
            });
        }

        // PUT: api/profile
        [HttpPut]
        public IActionResult UpdateProfile(
            [FromBody] UpdateProfileRequest request)
        {
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("User identity was not found.");
            }

            User? user = _userStore.Users
                .FirstOrDefault(u =>
                    u.Email.Equals(email,
                        StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return NotFound("Profile not found.");
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.DateOfBirth = request.DateOfBirth;

            return Ok("Profile updated successfully.");
        }
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}