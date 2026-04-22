using System;
using System.Collections.Generic;
using System.Linq;
using Jamaa.Application.Finances.Events;
using Jamaa.Application.Shared;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class AccountingPeriodRegenerationTests
{
    [Fact]
    public void ShouldYield12UniquePeriodsWhenFiscalYearExtendsFrom6MonthsTo12Months()
    {
        // Arrange: existing 6-month periods plus regenerated full-year periods.
        var oldHalfYearPeriods = BuildMonthlyPeriods(
            prefix: "old",
            new DateTime(2027, 1, 1),
            monthCount: 6);

        var regeneratedFullYearPeriods = BuildMonthlyPeriods(
            prefix: "new",
            new DateTime(2027, 1, 1),
            monthCount: 12);

        var mixedPeriods = oldHalfYearPeriods
            .Concat(regeneratedFullYearPeriods)
            .ToList();

        // Act
        var uniquePeriods = OrganisationProjection.BuildUniqueAccountingPeriods(mixedPeriods);

        // Assert
        uniquePeriods.Count.ShouldBe(12);
        uniquePeriods
            .Select(period => (period.StartDate.Date, period.EndDate.Date))
            .Distinct()
            .Count()
            .ShouldBe(12);

        uniquePeriods.First().StartDate.Date.ShouldBe(new DateTime(2027, 1, 1));
        uniquePeriods.Last().EndDate.Date.ShouldBe(new DateTime(2027, 12, 31));

        for (var index = 1; index < uniquePeriods.Count; index++)
        {
            uniquePeriods[index].StartDate.Date.ShouldBe(uniquePeriods[index - 1].EndDate.Date.AddDays(1));
        }
    }

    // Operation: builds contiguous monthly periods from a start date.
    private static IReadOnlyList<AccountingPeriodInfo> BuildMonthlyPeriods(string prefix, DateTime startDate, int monthCount)
    {
        var periods = new List<AccountingPeriodInfo>();
        var periodStart = startDate.Date;

        for (var sequence = 1; sequence <= monthCount; sequence++)
        {
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            periods.Add(new AccountingPeriodInfo(
                $"{prefix}-{sequence}",
                sequence,
                periodStart,
                periodEnd,
                false));

            periodStart = periodEnd.AddDays(1);
        }

        return periods;
    }
}


