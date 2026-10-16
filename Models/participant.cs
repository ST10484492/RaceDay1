using System.ComponentModel.DataAnnotations;

namespace RaceDay1.Models
{
    public class participant
    {
        [Key] public int ParticipantId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public DateTime RegistrationsDate { get; set; }

        // Links race participant to login account.
        public int? UserId { get; set; }
    }
}
