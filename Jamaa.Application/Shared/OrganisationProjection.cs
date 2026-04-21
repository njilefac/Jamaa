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

        readJournal.EventsByTag(LibotaEventTagger.OrganisationEvent, _state.LastOffset)
            .RunWith(sink, Context.Materializer());
    }

    private void RegisterCommandHandlers()
    {
        CommandAsync<EventEnvelope>(async e =>
        {
            _logger.LogInformation("Received envelope with offset [{Offset}]", e.Offset);
            var currentOffset = e.Offset;

            if (e.Event is ILibotaEvent evt)
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
                LibotaEventTagger.OrganisationEvent, val);
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

    private Task TryProcess(ILibotaEvent evt)
    {
        using var scope = _serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<JamaaDbContext>();

        return evt switch
        {
            OrganisationCreated organisationCreated => Handle(organisationCreated, dbContext),
            MemberRegistered memberRegistered => Handle(memberRegistered, dbContext),
            MemberUpdated memberUpdated => Handle(memberUpdated, dbContext),
            MemberRegistrationUpdated registrationUpdated => Handle(registrationUpdated, dbContext),
            MemberRegistrationEnded registrationEnded => Handle(registrationEnded, dbContext),
            FiscalYearCreated fiscalYearCreated => Handle(fiscalYearCreated, dbContext),
            FiscalYearUpdated fiscalYearUpdated => Handle(fiscalYearUpdated, dbContext),
            FiscalYearDeleted fiscalYearDeleted => Handle(fiscalYearDeleted, dbContext),
            AccountingPeriodCreated accountingPeriodCreated => Handle(accountingPeriodCreated, dbContext),
            AccountingPeriodUpdated accountingPeriodUpdated => Handle(accountingPeriodUpdated, dbContext),
            AccountingPeriodDeleted accountingPeriodDeleted => Handle(accountingPeriodDeleted, dbContext),
            _ => Task.CompletedTask
        };
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

        await dbContext.SaveChangesAsync();
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

        await dbContext.SaveChangesAsync();
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
        await dbContext.SaveChangesAsync();
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

        await dbContext.SaveChangesAsync();
    }

    private async Task Handle(AccountingPeriodUpdated @event, JamaaDbContext dbContext)
    {
        var period = await dbContext.AccountingPeriods.FirstOrDefaultAsync(current => current.Id == @event.AccountingPeriodId.Value);
        if (period is null)
        {
            return;
        }

        period.SequenceNumber = @event.SequenceNumber;
        period.StartDate = @event.StartDate;
        period.EndDate = @event.EndDate;
        period.IsLocked = @event.IsLocked;

        await dbContext.SaveChangesAsync();
    }

    private async Task Handle(AccountingPeriodDeleted @event, JamaaDbContext dbContext)
    {
        var period = await dbContext.AccountingPeriods.FirstOrDefaultAsync(current => current.Id == @event.AccountingPeriodId.Value);
        if (period is null)
        {
            return;
        }

        dbContext.AccountingPeriods.Remove(period);
        await dbContext.SaveChangesAsync();
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

        await dbContext.SaveChangesAsync();
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

            await dbContext.SaveChangesAsync();
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
        await dbContext.SaveChangesAsync();
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

    private void PersistAndAck(Offset currentOffset, ILibotaEvent evt)
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
}