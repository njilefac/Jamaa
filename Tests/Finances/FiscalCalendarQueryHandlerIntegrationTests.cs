using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Accounting.Queries;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Queries.Finances;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class FiscalCalendarQueryHandlerIntegrationTests
{
    [Fact]
    public async Task ShouldReturnUpdatedFiscalYearBoundariesAndRegeneratedPeriodsOnRequery()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"jamaa-fiscal-query-{Guid.NewGuid():N}.db");
        try
        {
            // Arrange
            var options = Options.Create(new DatabaseOptions { DataFile = databasePath });
            var organisationId = "org-1";
            var fiscalYearId = "fy-2027";
            var query = new GetFiscalYearsByOrganisation(OrganisationId.With(organisationId));

            await using (var setupContext = new JamaaDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();

                setupContext.FiscalYears.Add(new FiscalYearData
                {
                    Id = fiscalYearId,
                    OrganisationId = organisationId,
                    StartDate = new DateTime(2027, 1, 1),
                    EndDate = new DateTime(2027, 6, 30),
                    IsLocked = false
                });

                setupContext.AccountingPeriods.AddRange(BuildMonthlyPeriods(
                    organisationId,
                    fiscalYearId,
                    new DateTime(2027, 1, 1),
                    6));

                await setupContext.SaveChangesAsync();
            }

            await using var readerContext = new JamaaDbContext(options);
            var handler = new FiscalCalendarQueryHandler(readerContext);

            var firstRead = await handler.Get(query);
            firstRead.ShouldHaveSingleItem();
            firstRead[0].EndDate.Date.ShouldBe(new DateTime(2027, 6, 30));
            firstRead[0].Periods.Count.ShouldBe(6);

            // Simulate projection update in a different context: extend FY and regenerate periods.
            await using (var writerContext = new JamaaDbContext(options))
            {
                var fiscalYear = await writerContext.FiscalYears.FirstAsync(current => current.Id == fiscalYearId);
                fiscalYear.EndDate = new DateTime(2027, 12, 31);

                var oldPeriods = writerContext.AccountingPeriods.Where(period => period.FiscalYearId == fiscalYearId);
                writerContext.AccountingPeriods.RemoveRange(oldPeriods);
                await writerContext.SaveChangesAsync();

                writerContext.AccountingPeriods.AddRange(BuildMonthlyPeriods(
                    organisationId,
                    fiscalYearId,
                    new DateTime(2027, 1, 1),
                    12));
                await writerContext.SaveChangesAsync();
            }

            // Act
            var secondRead = await handler.Get(query);

            // Assert
            secondRead.ShouldHaveSingleItem();
            secondRead[0].StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
            secondRead[0].EndDate.Date.ShouldBe(new DateTime(2027, 12, 31));
            secondRead[0].Periods.Count.ShouldBe(12);
            secondRead[0].Periods
                .Select(period => (period.StartDate.Date, period.EndDate.Date))
                .Distinct()
                .Count()
                .ShouldBe(12);

            secondRead[0].Periods.First().StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
            secondRead[0].Periods.Last().EndDate.Date.ShouldBe(new DateTime(2027, 12, 31));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    // Operation: creates contiguous monthly accounting periods for one fiscal year.
    private static IReadOnlyList<AccountingPeriodData> BuildMonthlyPeriods(
        string organisationId,
        string fiscalYearId,
        DateTime startDate,
        int monthCount)
    {
        var periods = new List<AccountingPeriodData>();
        var currentStart = startDate.Date;

        for (var sequence = 1; sequence <= monthCount; sequence++)
        {
            var currentEnd = currentStart.AddMonths(1).AddDays(-1);
            periods.Add(new AccountingPeriodData
            {
                Id = $"{fiscalYearId}-p{sequence}",
                FiscalYearId = fiscalYearId,
                OrganisationId = organisationId,
                SequenceNumber = sequence,
                StartDate = currentStart,
                EndDate = currentEnd,
                IsLocked = false
            });

            currentStart = currentEnd.AddDays(1);
        }

        return periods;
    }
}