namespace RaceDay1.Models
{
    public class enrolment
    {
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }

        public int CategoryID { get; set; }

        public DateTime Enrolmentdate { get; set; }

        public string PayementStatus { get; set; } = string.Empty;

        public int RaceNumber { get; set; }
    }
}
