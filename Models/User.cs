using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay1.Models
{
    public class User
    {

        [Key]
        public int UserId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Participant";
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
