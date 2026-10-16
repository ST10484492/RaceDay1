
using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;

namespace RaceDay1
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(
            DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        public DbSet<Result> Results => Set<Result>();

        public DbSet<User> Users => Set<User>();

        public DbSet<organiser> Organisers => Set<organiser>();

        public DbSet<participant> Participants => Set<participant>();

        public DbSet<@event> Events => Set<@event>();

        public DbSet<categories> Categories => Set<categories>();

        public DbSet<enrolment> Enrolments => Set<enrolment>();
    }
}
