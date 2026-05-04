using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Values;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;
using Jamaa.Data.Models.Users;
using Jamaa.Data.Notifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jamaa.Data.Configuration;

public class JamaaDbContext(IOptions<DatabaseOptions> options, IDataChangeNotifier? dataChangeNotifier = null) : DbContext
{
    private readonly DatabaseOptions _dbOptions = options.Value;
    private readonly IDataChangeNotifier _dataChangeNotifier = dataChangeNotifier ?? NoOpDataChangeNotifier.Instance;
    public DbSet<UserData> Users { get; set; }
    public DbSet<OrganisationData> Organisations { get; set; }
    public DbSet<AccountData> Accounts { get; set; }
    public DbSet<FiscalYearData> FiscalYears { get; set; }
    public DbSet<AccountingPeriodData> AccountingPeriods { get; set; }
    public DbSet<AccountingSettingsData> AccountingSettings { get; set; }
    public DbSet<AccountingAvailableCurrencyData> AccountingAvailableCurrencies { get; set; }


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

        modelBuilder.Entity<FiscalYearData>().ToTable("FiscalYears");
        modelBuilder.Entity<FiscalYearData>()
            .Property(fiscalYear => fiscalYear.Id).IsRequired();
        modelBuilder.Entity<FiscalYearData>()
            .Property(fiscalYear => fiscalYear.OrganisationId).IsRequired();
        modelBuilder.Entity<FiscalYearData>()
            .HasMany(fiscalYear => fiscalYear.Periods)
            .WithOne(period => period.FiscalYear)
            .HasForeignKey(period => period.FiscalYearId);

        modelBuilder.Entity<AccountingPeriodData>().ToTable("AccountingPeriods");
        modelBuilder.Entity<AccountingPeriodData>()
            .Property(period => period.Id).IsRequired();
        modelBuilder.Entity<AccountingPeriodData>()
            .Property(period => period.OrganisationId).IsRequired();
        modelBuilder.Entity<AccountingPeriodData>()
            .HasIndex(period => new { period.OrganisationId, period.StartDate, period.EndDate })
            .IsUnique();

        modelBuilder.Entity<AccountingSettingsData>().ToTable("AccountingSettings");
        modelBuilder.Entity<AccountingSettingsData>()
            .HasKey(settings => settings.OrganisationId);
        modelBuilder.Entity<AccountingSettingsData>()
            .Property(settings => settings.BaseCurrency).IsRequired();
        modelBuilder.Entity<AccountingSettingsData>()
            .Property(settings => settings.DateFormat).IsRequired();
        modelBuilder.Entity<AccountingSettingsData>()
            .HasMany(settings => settings.AvailableCurrencies)
            .WithOne(currency => currency.AccountingSettings)
            .HasForeignKey(currency => currency.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AccountingAvailableCurrencyData>().ToTable("AccountingAvailableCurrencies");
        modelBuilder.Entity<AccountingAvailableCurrencyData>()
            .HasKey(currency => new { currency.OrganisationId, currency.CurrencyCode });
        modelBuilder.Entity<AccountingAvailableCurrencyData>()
            .Property(currency => currency.CurrencyCode).IsRequired();
        modelBuilder.Entity<AccountingAvailableCurrencyData>()
            .Property(currency => currency.CurrencySymbol).IsRequired();

        modelBuilder.Entity<AccountData>().ToTable("Accounts");
        modelBuilder.Entity<AccountData>()
            .Property(account => account.Id).IsRequired();
        modelBuilder.Entity<AccountData>()
            .Property(account => account.OrganisationId).IsRequired();
        modelBuilder.Entity<AccountData>()
            .Property(account => account.Code).IsRequired();
        modelBuilder.Entity<AccountData>()
            .Property(account => account.Name).IsRequired();
        modelBuilder.Entity<AccountData>()
            .Property(account => account.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<AccountData>()
            .HasOne(account => account.Parent)
            .WithMany()
            .HasForeignKey(account => account.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccountData>()
            .HasIndex(account => new { account.OrganisationId, account.Code })
            .IsUnique();
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
        modelBuilder.Entity<UserData>()
            .Property(e => e.DashboardLayout).IsRequired(false);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var committedChanges = CaptureTrackedChanges();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        if (result > 0)
        {
            PublishCommittedChanges(committedChanges);
        }

        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var committedChanges = CaptureTrackedChanges();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (result > 0)
        {
            PublishCommittedChanges(committedChanges);
        }

        return result;
    }

    // Operation: snapshots tracked entity transitions that should become change notifications after commit.
    private List<(EntityState State, object Entity)> CaptureTrackedChanges()
    {
        return ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => (entry.State, entry.Entity))
            .ToList();
    }

    // Operation: pushes post-commit changes to reactive notifier streams.
    private void PublishCommittedChanges(List<(EntityState State, object Entity)> committedChanges)
    {
        if (committedChanges.Count == 0)
        {
            return;
        }

        _dataChangeNotifier.NotifyCommittedChanges(committedChanges);
    }

    private sealed class NoOpDataChangeNotifier : IDataChangeNotifier
    {
        public static NoOpDataChangeNotifier Instance { get; } = new();

        public IObservable<object> Insertions => Observable.Empty<object>();
        public IObservable<object> Updates => Observable.Empty<object>();
        public IObservable<object> Deletions => Observable.Empty<object>();

        public void NotifyCommittedChanges(IEnumerable<(EntityState State, object Entity)> changes)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
        }
    }
}