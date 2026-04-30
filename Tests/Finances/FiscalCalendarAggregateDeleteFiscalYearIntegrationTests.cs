using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Aggregates;
using Jamaa.Application.Finances.Commands;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public sealed class FiscalCalendarAggregateDeleteFiscalYearIntegrationTests : IAsyncLifetime
{
    private readonly string _snapshotDir = Path.Combine(Path.GetTempPath(), $"jamaa-fiscal-aggregate-snap-{Guid.NewGuid():N}");
    private ActorSystem _actorSystem = null!;
    private IActorRef _aggregate = ActorRefs.Nobody;
    private readonly OrganisationId _organisationId = OrganisationId.With("org-fiscal-aggregate-delete-rule");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_snapshotDir);

        var config = ConfigurationFactory.ParseString($@"
            akka.loglevel = WARNING
            akka.actor.provider = local
            akka.persistence.journal.plugin = ""akka.persistence.journal.inmem""
            akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.local""
            akka.persistence.snapshot-store.local.dir = ""{_snapshotDir.Replace("\\", "\\\\")}""
        ");

        _actorSystem = ActorSystem.Create("fiscal-aggregate-delete-tests", config);
        _aggregate = _actorSystem.ActorOf(FiscalCalendarAggregate.Props(_organisationId), $"fiscal-calendar-{Guid.NewGuid():N}");

        // Seed a contiguous 3-year timeline.
        _aggregate.Tell(new CreateFiscalYear(_organisationId, FiscalYearId.With("fy-2022"), new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), false));
        _aggregate.Tell(new CreateFiscalYear(_organisationId, FiscalYearId.With("fy-2023"), new DateTime(2023, 1, 1), new DateTime(2023, 12, 31), false));
        _aggregate.Tell(new CreateFiscalYear(_organisationId, FiscalYearId.With("fy-2024"), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), false));

        await Task.Delay(250);
    }

    public async Task DisposeAsync()
    {
        await _actorSystem.Terminate();
        await _actorSystem.WhenTerminated;

        if (Directory.Exists(_snapshotDir))
        {
            Directory.Delete(_snapshotDir, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteFiscalYear_ShouldRejectMiddleYear_WhenDeletionWouldCreateGap()
    {
        var response = await _aggregate.Ask<string>(
            new DeleteFiscalYear(_organisationId, FiscalYearId.With("fy-2023")),
            timeout: TimeSpan.FromSeconds(3));

        response.ShouldBe("Deleting this fiscal year would create a gap.");
    }

    [Fact]
    public async Task DeleteFiscalYear_ShouldDeleteEdgeYear_ThroughCommandFlow()
    {
        _aggregate.Tell(new DeleteFiscalYear(_organisationId, FiscalYearId.With("fy-2024")));
        await Task.Delay(200);

        var response = await _aggregate.Ask<string>(
            new DeleteFiscalYear(_organisationId, FiscalYearId.With("fy-2024")),
            timeout: TimeSpan.FromSeconds(3));

        response.ShouldBe("Fiscal year not found.");
    }
}

