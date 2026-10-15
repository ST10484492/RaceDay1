using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;

namespace RaceDay1
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
        {

        }

        public DbSet<participant> Participants { get; set; }
        public DbSet<organiser> organisers { get; set; }
        public DbSet<RaceDay1.Models.@event> Events { get; set; }
        public DbSet<categories> Categories { get; set; }
        public DbSet<enrolment> Enrolments { get; set; }
    }
}
