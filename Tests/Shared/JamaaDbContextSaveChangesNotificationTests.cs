using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Domain.Shared.Values;
using Jamaa.Data.Configuration;
using Jamaa.Data.Notifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Shared;

public class JamaaDbContextSaveChangesNotificationTests
{
    [Fact]
    public void SaveChanges_ShouldNotPublish_WhenNoRowsAreAffected()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-savechanges-sync-{Guid.NewGuid():N}.db");

        try
        {
            var dataChangeNotifier = Substitute.For<IDataChangeNotifier>();
            using var dbContext = CreateDbContext(databasePath, dataChangeNotifier);
            dbContext.Database.EnsureCreated();

            var affectedRows = dbContext.SaveChanges(true);

            affectedRows.ShouldBe(0);
            dataChangeNotifier.DidNotReceive()
                .NotifyCommittedChanges(Arg.Any<IEnumerable<(EntityState State, object Entity)>>());
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotPublish_WhenNoRowsAreAffected()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-savechanges-async-{Guid.NewGuid():N}.db");

        try
        {
            var dataChangeNotifier = Substitute.For<IDataChangeNotifier>();
            await using var dbContext = CreateDbContext(databasePath, dataChangeNotifier);
            await dbContext.Database.EnsureCreatedAsync();

            var affectedRows = await dbContext.SaveChangesAsync(true);

            affectedRows.ShouldBe(0);
            dataChangeNotifier.DidNotReceive()
                .NotifyCommittedChanges(Arg.Any<IEnumerable<(EntityState State, object Entity)>>());
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    // Operation: builds one DbContext instance for focused SaveChanges notification tests.
    private static JamaaDbContext CreateDbContext(string databasePath, IDataChangeNotifier dataChangeNotifier)
    {
        var options = Options.Create(new DatabaseOptions
        {
            DataFile = databasePath
        });

        return new JamaaDbContext(options, dataChangeNotifier);
    }
}