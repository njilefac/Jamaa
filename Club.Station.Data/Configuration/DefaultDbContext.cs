using Domain.Entities;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;

namespace Club.Station.Data.Configuration
{
    public class DefaultDbContext : DbContextWithTriggers
    {
        public DbSet<Organization> Organizations { get; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
