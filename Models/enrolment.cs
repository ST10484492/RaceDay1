namespace RaceDay1.Models
{
    public class enrolment
    {
        public int EnrolmentId { get; set; }

        public int ParticipantId { get; set; }

        public int CategoryId { get; set; }

        public DateTime Enrolmentdate { get; set; }

        public string PayementStatus { get; set; } = string.Empty;

        public int RaceNumber { get; set; }
    }
}
