namespace Jamaa.Application.Finances.Aggregates;

public static class FiscalYearUpdatePlanner
{
    public sealed record FiscalYearWindow(string Id, DateTime StartDate, DateTime EndDate, bool IsLocked);

    public sealed record FiscalYearUpdatePlan(
        FiscalYearWindow Current,
        FiscalYearWindow? Previous,
        FiscalYearWindow? Next);

    // Integration: orchestrates fiscal-year boundary planning while preserving contiguity and lock rules.
    public static bool TryPlan(
        IReadOnlyCollection<FiscalYearWindow> fiscalYears,
        string targetFiscalYearId,
        DateTime requestedStartDate,
        DateTime requestedEndDate,
        bool targetIsLocked,
        out FiscalYearUpdatePlan? plan,
        out string? error)
    {
        plan = null;
        error = null;

        if (!TryValidateRequestedRange(requestedStartDate, requestedEndDate, out var startDate, out var endDate, out error))
        {
            return false;
        }

        if (!TryGetPlanningContext(fiscalYears, targetFiscalYearId, out var context, out error))
        {
            return false;
        }

        if (context is null)
        {
            error = "Fiscal year not found.";
            return false;
        }

        if (!TryPlanPreviousBoundary(context, startDate, out var previousPlan, out error))
        {
            return false;
        }

        if (!TryPlanNextBoundary(context, endDate, out var nextPlan, out error))
        {
            return false;
        }

        if (!TryValidateContiguity(context, startDate, endDate, previousPlan, nextPlan, out error))
        {
            return false;
        }

        plan = BuildUpdatePlan(context, startDate, endDate, targetIsLocked, previousPlan, nextPlan);

        return true;
    }

    // Operation: normalizes and validates the requested fiscal-year range.
    private static bool TryValidateRequestedRange(
        DateTime requestedStartDate,
        DateTime requestedEndDate,
        out DateTime startDate,
        out DateTime endDate,
        out string? error)
    {
        startDate = requestedStartDate.Date;
        endDate = requestedEndDate.Date;
        error = null;

        if (endDate >= startDate)
        {
            return true;
        }

        error = "Fiscal year end date must be on or after start date.";
        return false;
    }

    // Operation: resolves the ordered fiscal-year context around the target fiscal year.
    private static bool TryGetPlanningContext(
        IReadOnlyCollection<FiscalYearWindow> fiscalYears,
        string targetFiscalYearId,
        out PlanningContext? context,
        out string? error)
    {
        var orderedFiscalYears = fiscalYears
            .OrderBy(fiscalYear => fiscalYear.StartDate)
            .ToList();

        var currentIndex = orderedFiscalYears.FindIndex(fiscalYear => fiscalYear.Id == targetFiscalYearId);
        if (currentIndex < 0)
        {
            context = null;
            error = "Fiscal year not found.";
            return false;
        }

        context = new PlanningContext(
            orderedFiscalYears,
            orderedFiscalYears[currentIndex],
            currentIndex > 0 ? orderedFiscalYears[currentIndex - 1] : null,
            currentIndex < orderedFiscalYears.Count - 1 ? orderedFiscalYears[currentIndex + 1] : null);
        error = null;
        return true;
    }

    // Operation: calculates the previous fiscal-year adjustment when the target start date changes.
    private static bool TryPlanPreviousBoundary(
        PlanningContext context,
        DateTime startDate,
        out FiscalYearWindow? previousPlan,
        out string? error)
    {
        previousPlan = null;
        error = null;

        if (context.Previous is null)
        {
            return true;
        }

        var adjustedPreviousEndDate = startDate.AddDays(-1);
        if (adjustedPreviousEndDate < context.Previous.StartDate)
        {
            error = "Updating this fiscal year would invalidate the previous fiscal year range.";
            return false;
        }

        if (context.Previous.IsLocked && adjustedPreviousEndDate != context.Previous.EndDate)
        {
            error = "Adjacent locked fiscal years cannot be auto-adjusted.";
            return false;
        }

        if (adjustedPreviousEndDate == context.Previous.EndDate)
        {
            return true;
        }

        previousPlan = context.Previous with { EndDate = adjustedPreviousEndDate };
        return true;
    }

    // Operation: calculates the next fiscal-year adjustment when the target end date changes.
    private static bool TryPlanNextBoundary(
        PlanningContext context,
        DateTime endDate,
        out FiscalYearWindow? nextPlan,
        out string? error)
    {
        nextPlan = null;
        error = null;

        if (context.Next is null)
        {
            return true;
        }

        var adjustedNextStartDate = endDate.AddDays(1);
        if (adjustedNextStartDate > context.Next.EndDate)
        {
            error = "Updating this fiscal year would invalidate the next fiscal year range.";
            return false;
        }

        if (context.Next.IsLocked && adjustedNextStartDate != context.Next.StartDate)
        {
            error = "Adjacent locked fiscal years cannot be auto-adjusted.";
            return false;
        }

        if (adjustedNextStartDate == context.Next.StartDate)
        {
            return true;
        }

        nextPlan = context.Next with { StartDate = adjustedNextStartDate };
        return true;
    }

    // Operation: verifies that the planned fiscal-year windows remain contiguous.
    private static bool TryValidateContiguity(
        PlanningContext context,
        DateTime startDate,
        DateTime endDate,
        FiscalYearWindow? previousPlan,
        FiscalYearWindow? nextPlan,
        out string? error)
    {
        var candidateRanges = BuildCandidateRanges(context, startDate, endDate, previousPlan, nextPlan);
        if (IsContiguous(candidateRanges))
        {
            error = null;
            return true;
        }

        error = "Fiscal years must remain contiguous without gaps or overlaps.";
        return false;
    }

    // Operation: builds the candidate date ranges used for contiguity validation.
    private static List<(DateTime StartDate, DateTime EndDate)> BuildCandidateRanges(
        PlanningContext context,
        DateTime startDate,
        DateTime endDate,
        FiscalYearWindow? previousPlan,
        FiscalYearWindow? nextPlan)
    {
        return context.OrderedFiscalYears
            .Select(fiscalYear =>
            {
                if (fiscalYear.Id == context.Current.Id)
                {
                    return (StartDate: startDate, EndDate: endDate);
                }

                if (previousPlan is not null && fiscalYear.Id == previousPlan.Id)
                {
                    return (previousPlan.StartDate, previousPlan.EndDate);
                }

                if (nextPlan is not null && fiscalYear.Id == nextPlan.Id)
                {
                    return (nextPlan.StartDate, nextPlan.EndDate);
                }

                return (fiscalYear.StartDate, fiscalYear.EndDate);
            })
            .OrderBy(range => range.StartDate)
            .ToList();
    }

    // Operation: creates the final update plan from validated planning inputs.
    private static FiscalYearUpdatePlan BuildUpdatePlan(
        PlanningContext context,
        DateTime startDate,
        DateTime endDate,
        bool targetIsLocked,
        FiscalYearWindow? previousPlan,
        FiscalYearWindow? nextPlan)
    {
        return new FiscalYearUpdatePlan(
            context.Current with
            {
                StartDate = startDate,
                EndDate = endDate,
                IsLocked = targetIsLocked
            },
            previousPlan,
            nextPlan);
    }

    // Operation: checks whether date ranges form one contiguous, gap-free sequence.
    private static bool IsContiguous(IReadOnlyList<(DateTime StartDate, DateTime EndDate)> ranges)
    {
        if (ranges.Count <= 1)
        {
            return true;
        }

        if (ranges.Any(range => range.EndDate < range.StartDate))
        {
            return false;
        }

        for (var index = 1; index < ranges.Count; index++)
        {
            var previous = ranges[index - 1];
            var current = ranges[index];
            if (current.StartDate != previous.EndDate.AddDays(1))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record PlanningContext(
        IReadOnlyList<FiscalYearWindow> OrderedFiscalYears,
        FiscalYearWindow Current,
        FiscalYearWindow? Previous,
        FiscalYearWindow? Next);
}

