namespace RaceDay1.Models
{
    public class participant
    {
        public int ParticipantId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public DateTime RegistrationsDate { get; set; }
    }
}
