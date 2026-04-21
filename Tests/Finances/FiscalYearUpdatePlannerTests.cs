using System;
using System.Collections.Generic;
using Jamaa.Application.Finances.Aggregates;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class FiscalYearUpdatePlannerTests
{
    [Fact]
    public void ShouldAdjustPreviousFiscalYearWhenCurrentStartDateMovesForward()
    {
        // Arrange
        var fiscalYears = BuildContiguousYears();

        // Act
        var result = FiscalYearUpdatePlanner.TryPlan(
            fiscalYears,
            "fy-2",
            new DateTime(2025, 1, 10),
            new DateTime(2025, 12, 31),
            false,
            out var plan,
            out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeNull();
        plan.ShouldNotBeNull();
        plan.Previous.ShouldNotBeNull();
        plan.Previous.Id.ShouldBe("fy-1");
        plan.Previous.EndDate.ShouldBe(new DateTime(2025, 1, 9));
    }

    [Fact]
    public void ShouldAdjustNextFiscalYearWhenCurrentEndDateMovesBackward()
    {
        // Arrange
        var fiscalYears = BuildContiguousYears();

        // Act
        var result = FiscalYearUpdatePlanner.TryPlan(
            fiscalYears,
            "fy-2",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 20),
            false,
            out var plan,
            out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeNull();
        plan.ShouldNotBeNull();
        plan.Next.ShouldNotBeNull();
        plan.Next.Id.ShouldBe("fy-3");
        plan.Next.StartDate.ShouldBe(new DateTime(2025, 12, 21));
    }

    [Fact]
    public void ShouldRejectWhenPreviousFiscalYearWouldBecomeInvalid()
    {
        // Arrange
        var fiscalYears = new List<FiscalYearUpdatePlanner.FiscalYearWindow>
        {
            new("fy-1", new DateTime(2025, 1, 1), new DateTime(2025, 1, 5), false),
            new("fy-2", new DateTime(2025, 1, 6), new DateTime(2025, 12, 31), false)
        };

        // Act
        var result = FiscalYearUpdatePlanner.TryPlan(
            fiscalYears,
            "fy-2",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31),
            false,
            out var plan,
            out var error);

        // Assert
        result.ShouldBeFalse();
        plan.ShouldBeNull();
        error.ShouldBe("Updating this fiscal year would invalidate the previous fiscal year range.");
    }

    [Fact]
    public void ShouldRejectWhenAdjacentLockedFiscalYearNeedsAdjustment()
    {
        // Arrange
        var fiscalYears = new List<FiscalYearUpdatePlanner.FiscalYearWindow>
        {
            new("fy-1", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), false),
            new("fy-2", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), false),
            new("fy-3", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), true)
        };

        // Act
        var result = FiscalYearUpdatePlanner.TryPlan(
            fiscalYears,
            "fy-2",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 20),
            false,
            out var plan,
            out var error);

        // Assert
        result.ShouldBeFalse();
        plan.ShouldBeNull();
        error.ShouldBe("Adjacent locked fiscal years cannot be auto-adjusted.");
    }

    private static List<FiscalYearUpdatePlanner.FiscalYearWindow> BuildContiguousYears()
    {
        return
        [
            new("fy-1", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), false),
            new("fy-2", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), false),
            new("fy-3", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), false)
        ];
    }
}

