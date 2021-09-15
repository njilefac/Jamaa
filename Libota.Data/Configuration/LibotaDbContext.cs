using System.Reflection;
using Domain.Values;
using EntityFrameworkCore.Triggers;
using EventFlow.EntityFramework.Extensions;
using Hangfire.EntityFrameworkCore;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Queries.Models;
using Libota.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContext : DbContextWithTriggers
    {
        private readonly DatabaseOptions _dbOptions;
        public DbSet<UserData> Users { get; set; }
        public DbSet<OrganisationReadModel> Organisations { get; set; }


        public LibotaDbContext(IOptions<DatabaseOptions> options)
        {
            _dbOptions = options.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                $"Filename={_dbOptions.DataFile}",
                options => { options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName); });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddEventFlowEvents();
            modelBuilder.AddEventFlowSnapshots();
            modelBuilder.OnHangfireModelCreating();
            ConfigureUserMapping(modelBuilder);
            MapReadModels(modelBuilder);
        }

        private void MapReadModels(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganisationReadModel>()
                .Property(e => e.Id).ValueGeneratedOnAdd();
            
            modelBuilder.Entity<OrganisationReadModel>()
                .HasMany<Member>(e => e.Members)
                .WithOne(e => e.Organisation);

            modelBuilder.Entity<Member>().ToTable("Members")
                .Property(e => e.Id).ValueGeneratedOnAdd();
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