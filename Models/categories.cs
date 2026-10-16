using System.ComponentModel.DataAnnotations;

namespace RaceDay1.Models
{
    public class categories
    {
        [Key] public int CategoryId { get; set;  }

        public int EventID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public decimal EntryFee { get; set; }
    }
}
