using System;
using System.IO;
using System.Reflection;
using Domain.Values;
using EventFlow.EntityFramework.Extensions;
using Hangfire.EntityFrameworkCore;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Queries.Models;
using Libota.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration
{
    public class LibotaDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly DatabaseOptions _dbOptions;
        public DbSet<UserData> Users { get; set; }
        public DbSet<OrganisationReadModel> Organisations { get; set; }


        public LibotaDbContext(
            IServiceProvider serviceProvider,
            IOptions<DatabaseOptions> options,
            ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            _dbOptions = options.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var executingDirectory = Directory.GetCurrentDirectory();
            optionsBuilder.UseSqlite(
                $"Filename={executingDirectory}{Path.DirectorySeparatorChar}{_dbOptions.DataFile}",
                options => { options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName); });
            optionsBuilder.UseLoggerFactory(_loggerFactory);
            optionsBuilder.UseApplicationServiceProvider(_serviceProvider);
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

        private static void MapReadModels(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganisationReadModel>()
                .Property(e => e.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<OrganisationReadModel>()
                .HasMany(e => e.Members)
                .WithOne(e => e.Organisation)
                .HasForeignKey(m => m.OrganisationId);

            modelBuilder.Entity<Member>().ToTable("Members")
                .Property(e => e.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<Member>()
                .HasOne(x => x.Registration)
                .WithOne(r => r.Member)
                .HasForeignKey<Registration>(x => x.MemberId);

            modelBuilder.Entity<Registration>().ToTable("Registrations")
                .Property(e => e.Id).ValueGeneratedOnAdd();
        }

        private static void ConfigureUserMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserData>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<UserData>()
                .Property(e => e.Email).IsRequired(false);
            modelBuilder.Entity<UserData>()
                .HasIndex(e => e.Email).IsUnique();
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