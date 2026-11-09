using System.ComponentModel.DataAnnotations;

namespace RaceDay1.Models
{
    public class @event
    {
        [Key] public int EventId { get; set; }

        public int OrganiserId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // PART 3 additions from the database.sql)
        public string EventType { get; set; } = "Run";

        public decimal DistanceKm { get; set; }

        // Public Azure Blob URL of the banner image (null until uploaded).
        public string? BannerUrl { get; set; }
    }
}
