using System;
using System.IO;
using System.Threading.Tasks;
using Domain.Finances.Queries;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Queries.Finances;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class AccountingSettingsQueryHandlerIntegrationTests
{
    [Fact]
    public async Task ShouldReturnNullWhenNoSettingsExistForOrganisation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-acc-settings-{Guid.NewGuid():N}.db");
        try
        {
            var options = Options.Create(new DatabaseOptions { DataFile = databasePath });

            await using var ctx = new JamaaDbContext(options);
            await ctx.Database.EnsureCreatedAsync();

            var handler = new AccountingSettingsQueryHandler(ctx);
            var result = await handler.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With("org-missing")));

            result.ShouldBeNull();
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ShouldReturnPersistedSettingsForOrganisation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-acc-settings-{Guid.NewGuid():N}.db");
        try
        {
            var options = Options.Create(new DatabaseOptions { DataFile = databasePath });
            const string organisationId = "org-1";

            await using (var setupCtx = new JamaaDbContext(options))
            {
                await setupCtx.Database.EnsureCreatedAsync();
                setupCtx.AccountingSettings.Add(new AccountingSettingsData
                {
                    OrganisationId = organisationId,
                    BaseCurrency = "KES",
                    DateFormat = "DD/MM/YYYY",
                    DecimalPrecision = 2
                });
                await setupCtx.SaveChangesAsync();
            }

            await using var readerCtx = new JamaaDbContext(options);
            var handler = new AccountingSettingsQueryHandler(readerCtx);

            var result = await handler.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With(organisationId)));

            result.ShouldNotBeNull();
            result.OrganisationId.ShouldBe(organisationId);
            result.BaseCurrency.ShouldBe("KES");
            result.DateFormat.ShouldBe("DD/MM/YYYY");
            result.DecimalPrecision.ShouldBe(2);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ShouldReturnUpdatedSettingsAfterUpsert()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-acc-settings-{Guid.NewGuid():N}.db");
        try
        {
            var options = Options.Create(new DatabaseOptions { DataFile = databasePath });
            const string organisationId = "org-2";

            // Insert initial row
            await using (var setupCtx = new JamaaDbContext(options))
            {
                await setupCtx.Database.EnsureCreatedAsync();
                setupCtx.AccountingSettings.Add(new AccountingSettingsData
                {
                    OrganisationId = organisationId,
                    BaseCurrency = "USD",
                    DateFormat = "MM/DD/YYYY",
                    DecimalPrecision = 0
                });
                await setupCtx.SaveChangesAsync();
            }

            // Simulate projection update (upsert – update path)
            await using (var writerCtx = new JamaaDbContext(options))
            {
                var existing = await writerCtx.AccountingSettings.FindAsync(organisationId);
                existing!.BaseCurrency = "EUR";
                existing.DateFormat = "YYYY-MM-DD";
                existing.DecimalPrecision = 4;
                await writerCtx.SaveChangesAsync();
            }

            // Assert updated values are queryable
            await using var readerCtx = new JamaaDbContext(options);
            var handler = new AccountingSettingsQueryHandler(readerCtx);

            var result = await handler.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With(organisationId)));

            result.ShouldNotBeNull();
            result.BaseCurrency.ShouldBe("EUR");
            result.DateFormat.ShouldBe("YYYY-MM-DD");
            result.DecimalPrecision.ShouldBe(4);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ShouldReturnOnlySettingsForRequestedOrganisation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-acc-settings-{Guid.NewGuid():N}.db");
        try
        {
            var options = Options.Create(new DatabaseOptions { DataFile = databasePath });

            await using (var setupCtx = new JamaaDbContext(options))
            {
                await setupCtx.Database.EnsureCreatedAsync();
                setupCtx.AccountingSettings.AddRange(
                    new AccountingSettingsData { OrganisationId = "org-a", BaseCurrency = "USD", DateFormat = "DD/MM/YYYY", DecimalPrecision = 2 },
                    new AccountingSettingsData { OrganisationId = "org-b", BaseCurrency = "GBP", DateFormat = "MM/DD/YYYY", DecimalPrecision = 3 }
                );
                await setupCtx.SaveChangesAsync();
            }

            await using var readerCtx = new JamaaDbContext(options);
            var handler = new AccountingSettingsQueryHandler(readerCtx);

            var result = await handler.Get(new GetAccountingSettingsByOrganisation(OrganisationId.With("org-b")));

            result.ShouldNotBeNull();
            result.OrganisationId.ShouldBe("org-b");
            result.BaseCurrency.ShouldBe("GBP");
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}


