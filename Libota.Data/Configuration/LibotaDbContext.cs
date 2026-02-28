using System.Reflection;
using Domain.Shared.Values;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;
using Libota.Data.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Libota.Data.Configuration;

public class LibotaDbContext(IOptions<DatabaseOptions> options) : DbContext
{
    private readonly DatabaseOptions _dbOptions = options.Value;
    public DbSet<UserData> Users { get; set; }
    public DbSet<OrganisationData> Organisations { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(
            $"Filename={_dbOptions.DataFile}",
            options => { options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName); });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserMapping(modelBuilder);
        MapReadModels(modelBuilder);
    }

    private static void MapReadModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganisationData>()
            .Property(e => e.Id).IsRequired();

        modelBuilder.Entity<OrganisationData>()
            .Property(e => e.Name).IsRequired();
        
        modelBuilder.Entity<OrganisationData>()
            .Property(e => e.Description);

        modelBuilder.Entity<OrganisationData>()
            .HasMany(e => e.Members)
            .WithOne(e => e.Organisation)
            .HasForeignKey(m => m.OrganisationId);

        modelBuilder.Entity<MemberData>().ToTable("Members")
            .Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<MemberData>()
            .HasOne(x => x.Registration)
            .WithOne(r => r.Member)
            .HasForeignKey<RegistrationData>(x => x.MemberId);

        modelBuilder.Entity<RegistrationData>().ToTable("Registrations")
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