namespace RaceDay1.Models
{
    public class @event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
