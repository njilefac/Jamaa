using System.Reflection;
using Domain.Values;
using EntityFrameworkCore.Triggers;
using Libota.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContext : DbContextWithTriggers
    {
        private readonly DatabaseOptions _dbOptions;
        public DbSet<UserData> Users { get; set; }
        public DbSet<OrganisationData> Organisations { get; set; }


        public LibotaDbContext(IOptions<DatabaseOptions> options)
        {
            _dbOptions = options.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                $"Filename={_dbOptions.DataFile}",
                options =>
                {
                    options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureOrganisationMapping(modelBuilder);
            ConfigureUserMapping(modelBuilder);
        }

        private void ConfigureOrganisationMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganisationData>()
                .HasKey(e => e.Id);
            
            modelBuilder.Entity<OrganisationData>()
                .Property(e => e.Name).IsRequired();
            
            modelBuilder.Entity<OrganisationData>()
                .Property(e => e.Description).IsRequired(false);
        }
        

        private static void ConfigureUserMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserData>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<UserData>()
                .Property(e => e.Email).IsRequired(false);
            modelBuilder.Entity<UserData>()
                .Property(e => e.UserName).IsRequired();
            modelBuilder.Entity<UserData>()
                .Property(e => e.Password).IsRequired();
            modelBuilder.Entity<UserData>()
                .Property(e => e.MiddleName).IsRequired(false);
            modelBuilder.Entity<UserData>()
                .Property(e => e.LastName).IsRequired();
            modelBuilder.Entity<UserData>()
                .Property(e => e.Gender).IsRequired();
            modelBuilder.Entity<UserData>()
                .Property(e => e.IsActive).IsRequired();
            modelBuilder.Entity<UserData>()
                .Property(e => e.IsSuperUser).IsRequired();
        }
    }
}