using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Persistence;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Domain.Organisation.Values;
using Libota.Application.Members.Events;
using Libota.Application.Organisation.Events;
using Libota.Data.Configuration;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Shared;

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
        using var dbContext = scope.ServiceProvider.GetRequiredService<LibotaDbContext>();

        return evt switch
        {
            OrganisationCreated organisationCreated => Handle(organisationCreated, dbContext),
            MemberRegistered memberRegistered => Handle(memberRegistered, dbContext),
            MemberRegistrationUpdated registrationUpdated => Handle(registrationUpdated, dbContext),
            MemberRegistrationEnded registrationEnded => Handle(registrationEnded, dbContext),
            _ => Task.CompletedTask
        };
    }

    private async Task Handle(MemberRegistrationEnded @event, LibotaDbContext dbContext)
    {
        throw new NotImplementedException();
    }

    private async Task Handle(MemberRegistrationUpdated @event, LibotaDbContext dbContext)
    {
        throw new NotImplementedException();
    }

    private async Task Handle(MemberRegistered @event, LibotaDbContext dbContext)
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

    private async Task Handle(OrganisationCreated @event, LibotaDbContext dbContext)
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