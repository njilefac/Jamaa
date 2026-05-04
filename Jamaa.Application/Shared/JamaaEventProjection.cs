using Akka.Persistence;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Events;
using Jamaa.Application.Members.Events;
using Jamaa.Application.Organisation.Events;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jamaa.Application.Shared;

public class OrganisationProjection : ReceivePersistentActor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrganisationProjection> _logger;
    private MaterializedViewState _state = new(Offset.NoOffset());

    public OrganisationProjection(IServiceProvider serviceProvider, ILogger<OrganisationProjection> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        PersistenceId = "organisation-projection";

        RegisterEventHandlers();
        RegisterCommandHandlers();
    }

    protected override void OnReplaySuccess()
    {
        var readJournal = PersistenceQuery.Get(Context.System).ReadJournalFor<SqlReadJournal>("akka.persistence.query.journal.sql");
        var self = Self;
        var sink = Sink.ActorRefWithAck<EventEnvelope>(self, ProjectionStarting.Instance, ProjectionAck.Instance,
            ProjectionCompleted.Instance, ex => new ProjectionFailed(ex));

        readJournal.EventsByTag(JamaaEventTagger.OrganisationEvent, _state.LastOffset)
            .RunWith(sink, Context.Materializer());
    }

    private void RegisterCommandHandlers()
    {
        CommandAsync<EventEnvelope>(async e =>
        {
            _logger.LogInformation("Received envelope with offset [{Offset}]", e.Offset);
            var currentOffset = e.Offset;

            if (e.Event is IJamaaEvent evt)
            {
                await TryProcess(evt);
                PersistAndAck(currentOffset, evt);
                return;
            }

            _logger.LogWarning("Unsupported event [{EventType}] at offset [{Offset}] found by projector. Maybe this was tagged incorrectly?", e.Event, e.Offset);
            Sender.Tell(ProjectionAck.Instance, Self);
        });

        Command<ProjectionStarting>(_ => Sender.Tell(ProjectionAck.Instance, Self));

        Command<ProjectionFailed>(failed =>
        {
            var val = 0L;
            if (_state.LastOffset is Sequence seq)
                val = seq.Value;
            _logger.LogError(failed.Cause, "Projection FAILED for Tag [{Tag}] at Offset [{Offset}]",
                JamaaEventTagger.OrganisationEvent, val);
            throw new ApplicationException("Projection failed due to error. See InnerException for details.",
                failed.Cause);
        });

        Command<SaveSnapshotSuccess>(success =>
        {
            // purge older snapshots and messages
            DeleteMessages(success.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
    }

    private async Task TryProcess(IJamaaEvent evt)
    {
        using var scope = _serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<JamaaDbContext>();

        await (evt switch
        {
            OrganisationCreated organisationCreated => Handle(organisationCreated, dbContext),
            MemberRegistered memberRegistered => Handle(memberRegistered, dbContext),
            MemberUpdated memberUpdated => Handle(memberUpdated, dbContext),
            MemberRegistrationUpdated registrationUpdated => Handle(registrationUpdated, dbContext),
            MemberRegistrationEnded registrationEnded => Handle(registrationEnded, dbContext),
            AccountCreated accountCreated => Handle(accountCreated, dbContext),
            AccountUpdated accountUpdated => Handle(accountUpdated, dbContext),
            AccountDeleted accountDeleted => Handle(accountDeleted, dbContext),
            AccountDeactivated accountDeactivated => Handle(accountDeactivated, dbContext),
            AccountReactivated accountReactivated => Handle(accountReactivated, dbContext),
            FiscalYearCreated fiscalYearCreated => Handle(fiscalYearCreated, dbContext),
            FiscalYearUpdated fiscalYearUpdated => Handle(fiscalYearUpdated, dbContext),
            FiscalYearDeleted fiscalYearDeleted => Handle(fiscalYearDeleted, dbContext),
            FiscalYearPeriodsRegenerated fiscalYearPeriodsRegenerated => Handle(fiscalYearPeriodsRegenerated, dbContext),
            AccountingPeriodCreated accountingPeriodCreated => Handle(accountingPeriodCreated, dbContext),
            AccountingPeriodUpdated accountingPeriodUpdated => Handle(accountingPeriodUpdated, dbContext),
            AccountingPeriodDeleted accountingPeriodDeleted => Handle(accountingPeriodDeleted, dbContext),
            AccountingSettingsUpdated accountingSettingsUpdated => Handle(accountingSettingsUpdated, dbContext),
            _ => Task.CompletedTask
        });
    }

    // Operation: inserts a newly created chart-of-accounts row if it is not already projected.
    private async Task Handle(AccountCreated @event, JamaaDbContext dbContext)
    {
        var exists = await dbContext.Accounts.AnyAsync(account => account.Id == @event.AccountId.Value);
        if (exists)
        {
            return;
        }

        dbContext.Accounts.Add(new AccountData
        {
            Id = @event.AccountId.Value,
            OrganisationId = @event.OrganisationId.Value,
            Code = @event.Code,
            Name = @event.Name,
            Description = @event.Description,
            Type = @event.Type,
            ParentId = @event.ParentId?.Value
        });

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: updates one projected account row when account details change.
    private async Task Handle(AccountUpdated @event, JamaaDbContext dbContext)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(current => current.Id == @event.AccountId.Value);
        if (account is null)
        {
            return;
        }

        account.Code = @event.Code;
        account.Name = @event.Name;
        account.Description = @event.Description;
        account.Type = @event.Type;
        account.ParentId = @event.ParentId?.Value;

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: removes one projected account row when the account is deleted.
    private async Task Handle(AccountDeleted @event, JamaaDbContext dbContext)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(current => current.Id == @event.AccountId.Value);
        if (account is null)
        {
            return;
        }

        dbContext.Accounts.Remove(account);
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: marks one projected account row as inactive.
    private async Task Handle(AccountDeactivated @event, JamaaDbContext dbContext)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(current => current.Id == @event.AccountId.Value);
        if (account is null)
        {
            return;
        }

        account.IsActive = false;
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: marks one projected account row as active again.
    private async Task Handle(AccountReactivated @event, JamaaDbContext dbContext)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(current => current.Id == @event.AccountId.Value);
        if (account is null)
        {
            return;
        }

        account.IsActive = true;
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(FiscalYearCreated @event, JamaaDbContext dbContext)
    {
        var exists = await dbContext.FiscalYears.AnyAsync(fiscalYear => fiscalYear.Id == @event.FiscalYearId.Value);
        if (exists)
        {
            return;
        }

        dbContext.FiscalYears.Add(new FiscalYearData
        {
            Id = @event.FiscalYearId.Value,
            OrganisationId = @event.OrganisationId.Value,
            StartDate = @event.StartDate,
            EndDate = @event.EndDate,
            IsLocked = @event.IsLocked
        });

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(FiscalYearUpdated @event, JamaaDbContext dbContext)
    {
        var fiscalYear = await dbContext.FiscalYears
            .Include(current => current.Periods)
            .FirstOrDefaultAsync(current => current.Id == @event.FiscalYearId.Value);
        if (fiscalYear is null)
        {
            return;
        }

        fiscalYear.StartDate = @event.StartDate;
        fiscalYear.EndDate = @event.EndDate;
        fiscalYear.IsLocked = @event.IsLocked;

        if (fiscalYear.IsLocked)
        {
            foreach (var period in fiscalYear.Periods)
            {
                period.IsLocked = true;
            }
        }

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(FiscalYearDeleted @event, JamaaDbContext dbContext)
    {
        var fiscalYear = await dbContext.FiscalYears
            .Include(current => current.Periods)
            .FirstOrDefaultAsync(current => current.Id == @event.FiscalYearId.Value);

        if (fiscalYear is null)
        {
            return;
        }

        dbContext.AccountingPeriods.RemoveRange(fiscalYear.Periods);
        dbContext.FiscalYears.Remove(fiscalYear);
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: atomically replaces all periods for a fiscal year during regeneration.
    private async Task Handle(FiscalYearPeriodsRegenerated @event, JamaaDbContext dbContext)
    {
        var fiscalYear = await dbContext.FiscalYears
            .Include(current => current.Periods)
            .FirstOrDefaultAsync(current => current.Id == @event.FiscalYearId.Value);

        if (fiscalYear is null)
        {
            return;
        }

        // Replace all periods for the fiscal year so no stale rows remain in the read model.
        dbContext.AccountingPeriods.RemoveRange(fiscalYear.Periods);
        await SaveChangesWithSqliteRetryAsync(dbContext);

        var uniquePeriods = BuildUniqueAccountingPeriods(@event.CreatedPeriods);

        // Create all new periods, skipping any pre-existing natural-key duplicate.
        foreach (var periodData in uniquePeriods)
        {
            var existsByNaturalKey = await dbContext.AccountingPeriods.AnyAsync(period =>
                period.OrganisationId == @event.OrganisationId.Value &&
                period.StartDate == periodData.StartDate &&
                period.EndDate == periodData.EndDate);
            if (existsByNaturalKey)
            {
                continue;
            }

            dbContext.AccountingPeriods.Add(new AccountingPeriodData
            {
                Id = periodData.Id,
                FiscalYearId = @event.FiscalYearId.Value,
                OrganisationId = @event.OrganisationId.Value,
                SequenceNumber = periodData.SequenceNumber,
                StartDate = periodData.StartDate,
                EndDate = periodData.EndDate,
                IsLocked = periodData.IsLocked
            });
        }

        // Save all changes atomically
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: deduplicates periods by date-range key and keeps deterministic sequence ordering.
    public static IReadOnlyList<AccountingPeriodInfo> BuildUniqueAccountingPeriods(IReadOnlyList<AccountingPeriodInfo> periods)
    {
        return periods
            .GroupBy(period => new { StartDate = period.StartDate.Date, EndDate = period.EndDate.Date })
            .Select(group => group.OrderBy(period => period.SequenceNumber).First())
            .OrderBy(period => period.SequenceNumber)
            .ToList();
    }

    private async Task Handle(AccountingPeriodCreated @event, JamaaDbContext dbContext)
    {
        var fiscalYear = await dbContext.FiscalYears.FirstOrDefaultAsync(current => current.Id == @event.FiscalYearId.Value);
        if (fiscalYear is null)
        {
            return;
        }

        var exists = await dbContext.AccountingPeriods.AnyAsync(period => period.Id == @event.AccountingPeriodId.Value);
        if (exists)
        {
            return;
        }

        var duplicateNaturalKeyExists = await dbContext.AccountingPeriods.AnyAsync(period =>
            period.OrganisationId == @event.OrganisationId.Value &&
            period.StartDate == @event.StartDate &&
            period.EndDate == @event.EndDate);
        if (duplicateNaturalKeyExists)
        {
            return;
        }

        dbContext.AccountingPeriods.Add(new AccountingPeriodData
        {
            Id = @event.AccountingPeriodId.Value,
            FiscalYearId = @event.FiscalYearId.Value,
            OrganisationId = @event.OrganisationId.Value,
            SequenceNumber = @event.SequenceNumber,
            StartDate = @event.StartDate,
            EndDate = @event.EndDate,
            IsLocked = @event.IsLocked
        });

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(AccountingPeriodUpdated @event, JamaaDbContext dbContext)
    {
        var period = await dbContext.AccountingPeriods.FirstOrDefaultAsync(current => current.Id == @event.AccountingPeriodId.Value);
        if (period is null)
        {
            return;
        }

        var duplicateNaturalKeyExists = await dbContext.AccountingPeriods.AnyAsync(current =>
            current.Id != @event.AccountingPeriodId.Value &&
            current.OrganisationId == @event.OrganisationId.Value &&
            current.StartDate == @event.StartDate &&
            current.EndDate == @event.EndDate);
        if (duplicateNaturalKeyExists)
        {
            return;
        }

        period.SequenceNumber = @event.SequenceNumber;
        period.StartDate = @event.StartDate;
        period.EndDate = @event.EndDate;
        period.IsLocked = @event.IsLocked;

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(AccountingPeriodDeleted @event, JamaaDbContext dbContext)
    {
        var period = await dbContext.AccountingPeriods.FirstOrDefaultAsync(current => current.Id == @event.AccountingPeriodId.Value);
        if (period is null)
        {
            return;
        }

        dbContext.AccountingPeriods.Remove(period);
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    // Operation: upserts the accounting settings read model row for one organisation.
    private async Task Handle(AccountingSettingsUpdated @event, JamaaDbContext dbContext)
    {
        var existing = await dbContext.AccountingSettings
            .Include(settings => settings.AvailableCurrencies)
            .FirstOrDefaultAsync(settings => settings.OrganisationId == @event.OrganisationId.Value);

        if (existing is null)
        {
            dbContext.AccountingSettings.Add(new Data.Models.Finances.AccountingSettingsData
            {
                OrganisationId = @event.OrganisationId.Value,
                BaseCurrency = @event.BaseCurrency,
                DateFormat = @event.DateFormat,
                DecimalPrecision = @event.DecimalPrecision,
                AvailableCurrencies = (@event.AvailableCurrencies ?? [])
                    .Select(currency => new AccountingAvailableCurrencyData
                    {
                        OrganisationId = @event.OrganisationId.Value,
                        CurrencyCode = currency.Code,
                        CurrencySymbol = currency.Symbol
                    })
                    .ToList()
            });
        }
        else
        {
            existing.BaseCurrency = @event.BaseCurrency;
            existing.DateFormat = @event.DateFormat;
            existing.DecimalPrecision = @event.DecimalPrecision;

            dbContext.AccountingAvailableCurrencies.RemoveRange(existing.AvailableCurrencies);
            existing.AvailableCurrencies = (@event.AvailableCurrencies ?? [])
                .Select(currency => new AccountingAvailableCurrencyData
                {
                    OrganisationId = @event.OrganisationId.Value,
                    CurrencyCode = currency.Code,
                    CurrencySymbol = currency.Symbol
                })
                .ToList();
        }

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(MemberRegistrationEnded @event, JamaaDbContext dbContext)
    {
        // Currently no additional data is carried with this event. Mark the registration as ended if we can resolve the member.
        // Best-effort no-op to keep projector healthy until richer event payloads are introduced.
        await Task.CompletedTask;
    }

    private async Task Handle(MemberRegistrationUpdated @event, JamaaDbContext dbContext)
    {
        // This legacy event carries only the member id. Without additional payload, we cannot update any fields.
        // Avoid throwing and keep the projection advancing.
        await Task.CompletedTask;
    }

    private async Task Handle(MemberUpdated @event, JamaaDbContext dbContext)
    {
        // Best-effort update: try to locate the member by Id; if not found (due to earlier auto-generated ids),
        // fall back to matching by organisation and name as a heuristic.
        var member = await dbContext.Set<MemberData>()
            .Include(m => m.Registration)
            .FirstOrDefaultAsync(m => m.Id == @event.Id.Value);

        if (member == null)
        {
            member = await dbContext.Set<MemberData>()
                .Include(m => m.Registration)
                .FirstOrDefaultAsync(m => m.OrganisationId == @event.OrganisationId.Value
                                           && m.FirstName == @event.FirstName
                                           && m.LastName == @event.LastName);
        }

        if (member == null)
        {
            // Nothing to update in read model
            return;
        }

        member.FirstName = @event.FirstName;
        member.MiddleName = @event.MiddleName;
        member.LastName = @event.LastName;
        member.Gender = @event.Gender;
        member.PictureData = @event.Avatar;

        if (member.Registration != null)
        {
            member.Registration.StartDate = @event.RegistrationBegin;
            member.Registration.EndDate = @event.RegistrationEnd;
            member.Registration.MembershipType = @event.MembershipType;
            member.Registration.Status = @event.Status;
        }

        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private async Task Handle(MemberRegistered @event, JamaaDbContext dbContext)
    {
        var matchingOrganisation = await dbContext.Organisations.FirstOrDefaultAsync(x => x.Id == @event.EntityId);
        if (matchingOrganisation != null)
        {
            var registrationData = new RegistrationData
            {
                Organisation = matchingOrganisation,
                MembershipType = @event.MembershipType,
                StartDate = @event.RegistrationBegin,
                Status = RegistrationStatus.Full
            };
            var memberData = new MemberData
            {
                FirstName = @event.FirstName,
                MiddleName = @event.MiddleName,
                LastName = @event.LastName,
                Gender = @event.Gender,
                OrganisationId = @event.EntityId,
                Organisation = matchingOrganisation,
                Registration = registrationData
            };

            matchingOrganisation.Members.Add(memberData);

            await SaveChangesWithSqliteRetryAsync(dbContext);
        }
    }

    private async Task Handle(OrganisationCreated @event, JamaaDbContext dbContext)
    {
        dbContext.Organisations.Add(new OrganisationData
        {
            Id = @event.Id.Value,
            Name = @event.Name,
            Description = @event.Description,
            Members = new List<MemberData>()
        });
        await SaveChangesWithSqliteRetryAsync(dbContext);
    }

    private void RegisterEventHandlers()
    {
        Recover<MaterializedViewState>(s => { _state = s; });

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is MaterializedViewState state)
                _state = state;
        });
    }

    private void PersistAndAck(Offset currentOffset, IJamaaEvent evt)
    {
        var nextState = new MaterializedViewState(LastOffset: currentOffset);
        Persist(nextState, state =>
        {
            _state = state;
            _logger.LogInformation("Successfully processed event [{Event}] - projection state updated to [{Offset}]", evt,
                currentOffset);
            Sender.Tell(ProjectionAck.Instance, Self);

            if (LastSequenceNr % 10 == 0)
            {
                SaveSnapshot(_state);
            }
        });
    }

    public override string PersistenceId { get; }

    public sealed record ProjectionFailed(Exception Cause);

    public sealed class ProjectionCompleted
    {
        public static readonly ProjectionCompleted Instance = new();
    }

    public sealed class ProjectionAck
    {
        public static readonly ProjectionAck Instance = new();
    }

    public sealed class ProjectionStarting
    {
        public static readonly ProjectionStarting Instance = new();
    }

    public record MaterializedViewState(Offset LastOffset);

    // Operation: persists projection writes with transient SQLite lock retries.
    private async Task SaveChangesWithSqliteRetryAsync(JamaaDbContext dbContext)
    {
        const int maxAttempts = 4;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync();
                return;
            }
            catch (Exception exception) when (IsTransientSqliteLock(exception) && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(75 * attempt);
                _logger.LogWarning(
                    exception,
                    "Transient SQLite lock while saving projection changes. Retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts}).",
                    delay.TotalMilliseconds,
                    attempt,
                    maxAttempts);

                await Task.Delay(delay);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    // Operation: identifies retry-safe SQLite lock/busy failures.
    private static bool IsTransientSqliteLock(Exception exception)
    {
        return exception switch
        {
            SqliteException sqliteException => sqliteException.SqliteErrorCode is 5 or 6,
            DbUpdateException dbUpdateException when dbUpdateException.InnerException is SqliteException sqliteException
                => sqliteException.SqliteErrorCode is 5 or 6,
            _ => false
        };
    }
}