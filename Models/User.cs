using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay1.Models
{
    public class User
    {
        // Primary key for the authentication user.
        [Key]
        public int UserId { get; set; }

        // Matches dbo.Users.FirstName.
        [Required]
        public string FirstName { get; set; } = string.Empty;

        // Used for login and account identification.
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Stores the hashed password, never the plain-text password.
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Examples: Participant and Organiser.
        public string Role { get; set; } = "Participant";

        // Matches the nullable DateOfBirth column in dbo.Users.
        public DateTime? DateOfBirth { get; set; }
    }
}
