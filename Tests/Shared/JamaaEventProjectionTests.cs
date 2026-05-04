using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Organisation.Events;
using Jamaa.Application.Shared;
using Jamaa.Data.Configuration;
using Jamaa.Data.Notifiers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Shared;

public class OrganisationProjectionTests
{
    [Fact]
    public async Task TryProcess_ShouldKeepDbContextAlive_DuringSqliteRetry()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-projection-{Guid.NewGuid():N}.db");
        var actorSystem = CreateActorSystem();

        try
        {
            var serviceProvider = BuildServiceProvider(databasePath);
            await EnsureDatabaseCreatedAsync(serviceProvider);

            await using var lockConnection = new SqliteConnection($"Data Source={databasePath}");
            await lockConnection.OpenAsync();
            await using (var beginExclusive = lockConnection.CreateCommand())
            {
                beginExclusive.CommandText = "BEGIN EXCLUSIVE;";
                await beginExclusive.ExecuteNonQueryAsync();
            }

            var logger = Substitute.For<ILogger<OrganisationProjection>>();
            var projection = actorSystem.ActorOf(
                Props.Create(() => new TestableOrganisationProjection(serviceProvider, logger)),
                $"organisation-projection-test-{Guid.NewGuid():N}");
            var organisationId = OrganisationId.With(Guid.NewGuid().ToString());
            var organisationCreated = new OrganisationCreated(organisationId, "Retry Org", "Projection retry test");
            var processingTask = projection.Ask<ProcessCompleted>(
                new ProcessEvent(organisationCreated),
                TimeSpan.FromSeconds(10));

            // Let the first save attempt hit the SQLite lock, then release so a retry can succeed.
            await Task.Delay(250);
            await using (var commit = lockConnection.CreateCommand())
            {
                commit.CommandText = "COMMIT;";
                await commit.ExecuteNonQueryAsync();
            }

            var completed = await Task.WhenAny(processingTask, Task.Delay(TimeSpan.FromSeconds(5)));
            completed.ShouldBe(processingTask);
            (await processingTask).ShouldBe(ProcessCompleted.Instance);

            await using var verificationScope = serviceProvider.CreateAsyncScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<JamaaDbContext>();
            var projected = await dbContext.Organisations.SingleOrDefaultAsync(org => org.Id == organisationId.Value);
            projected.ShouldNotBeNull();
            projected.Name.ShouldBe("Retry Org");
        }
        finally
        {
            await actorSystem.Terminate();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    // Integration: builds a minimal service provider for projection processing with a real SQLite DbContext.
    private static ServiceProvider BuildServiceProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<IOptions<DatabaseOptions>>(_ => Options.Create(new DatabaseOptions
        {
            DataFile = databasePath
        }));
        services.AddDbContext<JamaaDbContext>();
        return services.BuildServiceProvider();
    }

    private static async Task EnsureDatabaseCreatedAsync(ServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JamaaDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static ActorSystem CreateActorSystem()
    {
        var config = ConfigurationFactory.ParseString(@"
            akka.persistence.journal.plugin = ""akka.persistence.journal.inmem""
            akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.local""
            akka.persistence.snapshot-store.local.dir = ""target/snapshots-tests""
        ");

        return ActorSystem.Create($"organisation-projection-tests-{Guid.NewGuid():N}", config);
    }

    private sealed class TestableOrganisationProjection : OrganisationProjection
    {
        private static readonly MethodInfo TryProcessMethod = typeof(OrganisationProjection)
            .GetMethod("TryProcess", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public TestableOrganisationProjection(IServiceProvider serviceProvider, ILogger<OrganisationProjection> logger)
            : base(serviceProvider, logger)
        {
            CommandAsync<ProcessEvent>(async command =>
            {
                var processingTask = (Task)TryProcessMethod.Invoke(this, [command.Event])!;
                await processingTask;
                Sender.Tell(ProcessCompleted.Instance);
            });
        }

        protected override void OnReplaySuccess()
        {
            // No-op in focused tests; this avoids starting read-journal streams.
        }
    }

    private sealed record ProcessEvent(IJamaaEvent Event);

    private sealed record ProcessCompleted
    {
        public static ProcessCompleted Instance { get; } = new();
    }
}




