using Akka.Actor;
using Akka.Persistence;
using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Finances.Commands;
using Jamaa.Application.Finances.Events;

namespace Jamaa.Application.Finances.Aggregates;

public class FiscalCalendarAggregate : ReceivePersistentActor
{
    private readonly FiscalCalendarState _state = new();

    public FiscalCalendarAggregate(OrganisationId organisationId)
    {
        PersistenceId = $"fiscal-calendar-{organisationId.Value}";

        RegisterCommandHandlers();
        RegisterEventHandlers();
    }

    public override string PersistenceId { get; }

    public static Props Props(OrganisationId organisationId)
    {
        return new Props(typeof(FiscalCalendarAggregate), [organisationId]);
    }

    private void RegisterCommandHandlers()
    {
        Command<CreateFiscalYear>(Handle);
        Command<UpdateFiscalYear>(Handle);
        Command<DeleteFiscalYear>(Handle);
        Command<CreateAccountingPeriod>(Handle);
        Command<UpdateAccountingPeriod>(Handle);
        Command<DeleteAccountingPeriod>(Handle);
    }

    private void RegisterEventHandlers()
    {
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is FiscalCalendarState state)
            {
                _state.CopyFrom(state);
            }
        });

        Recover<FiscalYearCreated>(Apply);
        Recover<FiscalYearUpdated>(Apply);
        Recover<FiscalYearDeleted>(Apply);
        Recover<AccountingPeriodCreated>(Apply);
        Recover<AccountingPeriodUpdated>(Apply);
        Recover<AccountingPeriodDeleted>(Apply);
        Recover<FiscalYearPeriodsRegenerated>(Apply);
    }

    // Integration: orchestrates creation of a fiscal year and its default contiguous periods.
    private void Handle(CreateFiscalYear command)
    {
        if (!TryGetValidFiscalYearRange(command.StartDate, command.EndDate, out var startDate, out var endDate))
        {
            return;
        }

        if (!TryValidateFiscalYearCreation(command.FiscalYearId.Value, startDate, endDate))
        {
            return;
        }

        PersistCreatedFiscalYear(command, startDate, endDate);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: updates fiscal year boundaries and regenerates periods to preserve full-year coverage.
    private void Handle(UpdateFiscalYear command)
    {
        if (!TryGetExistingFiscalYear(command.FiscalYearId.Value, out var existingFiscalYear))
        {
            return;
        }

        if (!TryGetValidFiscalYearRange(command.StartDate, command.EndDate, out var startDate, out var endDate))
        {
            return;
        }

        if (!TryGetFiscalYearUpdatePlan(command, startDate, endDate, out var plan))
        {
            return;
        }

        PersistPlannedFiscalYearUpdates(command.OrganisationId, existingFiscalYear, plan);

        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Operation: updates one fiscal year and fully regenerates its accounting periods as a single atomic event.
    private void PersistFiscalYearWithRegeneratedPeriods(
        OrganisationId organisationId,
        FiscalYearState fiscalYear,
        DateTime startDate,
        DateTime endDate,
        bool isLocked)
    {
        Persist(new FiscalYearUpdated(
            organisationId,
            FiscalYearId.With(fiscalYear.Id),
            startDate,
            endDate,
            isLocked), Apply);

        var deletedPeriodIds = fiscalYear.Periods.Values
            .Select(period => period.Id)
            .ToList();

        var generatedPeriodData = GenerateDefaultPeriods(
                organisationId,
                FiscalYearId.With(fiscalYear.Id),
                startDate,
                endDate,
                isLocked)
            .Select(evt => new AccountingPeriodInfo(
                evt.AccountingPeriodId.Value,
                evt.SequenceNumber,
                evt.StartDate,
                evt.EndDate,
                evt.IsLocked))
            .ToList();

        // Emit a single atomic event for period regeneration to avoid intermediate states in the read model.
        Persist(new FiscalYearPeriodsRegenerated(
            organisationId,
            FiscalYearId.With(fiscalYear.Id),
            deletedPeriodIds,
            generatedPeriodData), Apply);

        // Apply the new periods to the aggregate state.
        foreach (var periodData in generatedPeriodData)
        {
            fiscalYear.Periods[periodData.Id] = new AccountingPeriodState
            {
                Id = periodData.Id,
                SequenceNumber = periodData.SequenceNumber,
                StartDate = periodData.StartDate,
                EndDate = periodData.EndDate,
                IsLocked = periodData.IsLocked
            };
        }

        // Clear deleted periods from the aggregate state.
        foreach (var periodId in deletedPeriodIds)
        {
            fiscalYear.Periods.Remove(periodId);
        }
    }

    // Integration: removes a fiscal year when resulting timeline remains contiguous.
    private void Handle(DeleteFiscalYear command)
    {
        if (!TryGetExistingFiscalYear(command.FiscalYearId.Value, out var fiscalYear))
        {
            PersistDeletedFiscalYearWhenMissing(command);
            DeferAsync(true, _ => TrySaveSnapshot());
            return;
        }

        if (!TryValidateFiscalYearDeletion(command.FiscalYearId.Value))
        {
            return;
        }

        PersistDeletedFiscalYear(command, fiscalYear);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Operation: emits an idempotent fiscal-year deletion event when aggregate state is missing the target id.
    private void PersistDeletedFiscalYearWhenMissing(DeleteFiscalYear command)
    {
        Persist(new FiscalYearDeleted(command.OrganisationId, command.FiscalYearId), Apply);
    }

    // Operation: resolves one fiscal year from aggregate state.
    private bool TryGetExistingFiscalYear(string fiscalYearId, out FiscalYearState fiscalYear)
    {
        if (_state.FiscalYears.TryGetValue(fiscalYearId, out fiscalYear!))
        {
            return true;
        }

        Sender.Tell("Fiscal year not found.", Self);
        fiscalYear = null!;
        return false;
    }

    // Operation: normalizes and validates one fiscal-year date range.
    private bool TryGetValidFiscalYearRange(DateTime requestedStartDate, DateTime requestedEndDate, out DateTime startDate, out DateTime endDate)
    {
        startDate = requestedStartDate.Date;
        endDate = requestedEndDate.Date;

        if (endDate >= startDate)
        {
            return true;
        }

        Sender.Tell("Fiscal year end date must be on or after start date.", Self);
        return false;
    }

    // Operation: validates whether a new fiscal year can be added without breaking contiguity.
    private bool TryValidateFiscalYearCreation(string fiscalYearId, DateTime startDate, DateTime endDate)
    {
        if (_state.FiscalYears.ContainsKey(fiscalYearId))
        {
            Sender.Tell("Fiscal year already exists.", Self);
            return false;
        }

        var candidateFiscalYears = _state.FiscalYears.Values
            .Select(ToDateRange)
            .Append(new DateRange(startDate, endDate))
            .ToList();

        if (IsContiguous(candidateFiscalYears))
        {
            return true;
        }

        Sender.Tell("Fiscal years must remain contiguous without gaps or overlaps.", Self);
        return false;
    }

    // Integration: persists a new fiscal year together with its generated accounting periods.
    private void PersistCreatedFiscalYear(CreateFiscalYear command, DateTime startDate, DateTime endDate)
    {
        var created = new FiscalYearCreated(
            command.OrganisationId,
            command.FiscalYearId,
            startDate,
            endDate,
            command.IsLocked);

        Persist(created, Apply);

        var generatedPeriods = GenerateDefaultPeriods(
            command.OrganisationId,
            command.FiscalYearId,
            startDate,
            endDate,
            command.IsLocked).ToList();
        PersistAll(generatedPeriods, Apply);
    }

    // Operation: builds the planner input and resolves an update plan.
    private bool TryGetFiscalYearUpdatePlan(UpdateFiscalYear command, DateTime startDate, DateTime endDate, out FiscalYearUpdatePlanner.FiscalYearUpdatePlan plan)
    {
        var plannerInput = _state.FiscalYears.Values
            .Select(fiscalYear => new FiscalYearUpdatePlanner.FiscalYearWindow(
                fiscalYear.Id,
                fiscalYear.StartDate,
                fiscalYear.EndDate,
                fiscalYear.IsLocked))
            .ToList();

        if (FiscalYearUpdatePlanner.TryPlan(
                plannerInput,
                command.FiscalYearId.Value,
                startDate,
                endDate,
                command.IsLocked,
                out var resolvedPlan,
                out var error) &&
            resolvedPlan is not null)
        {
            plan = resolvedPlan;
            return true;
        }

        Sender.Tell(error ?? "Fiscal years must remain contiguous without gaps or overlaps.", Self);
        plan = null!;
        return false;
    }

    // Integration: applies all fiscal-year updates described by a validated plan.
    private void PersistPlannedFiscalYearUpdates(
        OrganisationId organisationId,
        FiscalYearState existingFiscalYear,
        FiscalYearUpdatePlanner.FiscalYearUpdatePlan plan)
    {
        PersistFiscalYearWithRegeneratedPeriods(
            organisationId,
            existingFiscalYear,
            plan.Current.StartDate,
            plan.Current.EndDate,
            plan.Current.IsLocked);

        PersistOptionalFiscalYearUpdate(organisationId, plan.Previous);
        PersistOptionalFiscalYearUpdate(organisationId, plan.Next);
    }

    // Operation: persists one optional planned fiscal-year update when its target exists.
    private void PersistOptionalFiscalYearUpdate(OrganisationId organisationId, FiscalYearUpdatePlanner.FiscalYearWindow? plannedFiscalYear)
    {
        if (plannedFiscalYear is null || !_state.FiscalYears.TryGetValue(plannedFiscalYear.Id, out var fiscalYear))
        {
            return;
        }

        PersistFiscalYearWithRegeneratedPeriods(
            organisationId,
            fiscalYear,
            plannedFiscalYear.StartDate,
            plannedFiscalYear.EndDate,
            plannedFiscalYear.IsLocked);
    }

    // Operation: validates whether deleting one fiscal year keeps the remaining timeline contiguous.
    private bool TryValidateFiscalYearDeletion(string fiscalYearId)
    {
        var remaining = _state.FiscalYears.Values
            .Where(current => current.Id != fiscalYearId)
            .Select(ToDateRange)
            .ToList();

        if (remaining.Count <= 1 || IsContiguous(remaining))
        {
            return true;
        }

        Sender.Tell("Deleting this fiscal year would create a gap.", Self);
        return false;
    }

    // Integration: persists fiscal-year deletion together with removal of its accounting periods.
    private void PersistDeletedFiscalYear(DeleteFiscalYear command, FiscalYearState fiscalYear)
    {
        var deletedPeriods = fiscalYear.Periods.Values
            .Select(period => new AccountingPeriodDeleted(
                command.OrganisationId,
                command.FiscalYearId,
                AccountingPeriodId.With(period.Id)))
            .ToList();
        PersistAll(deletedPeriods, Apply);

        Persist(new FiscalYearDeleted(command.OrganisationId, command.FiscalYearId), Apply);
    }

    // Integration: adds a period after validating fiscal-year coverage and contiguity rules.
    private void Handle(CreateAccountingPeriod command)
    {
        if (!TryGetUnlockedFiscalYear(command.FiscalYearId.Value, out var fiscalYear))
            return;

        if (!TryValidateAccountingPeriodCreation(command, fiscalYear))
            return;

        PersistCreatedAccountingPeriod(command, fiscalYear);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: updates a period after validating full fiscal-year coverage is preserved.
    private void Handle(UpdateAccountingPeriod command)
    {
        if (!TryGetUnlockedFiscalYear(command.FiscalYearId.Value, out var fiscalYear))
            return;

        if (!TryGetUnlockedAccountingPeriod(fiscalYear, command.AccountingPeriodId.Value))
            return;

        if (!TryValidateAccountingPeriodUpdate(command, fiscalYear))
            return;

        PersistUpdatedAccountingPeriod(command, fiscalYear);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Integration: deletes a period after validating full fiscal-year coverage remains intact.
    private void Handle(DeleteAccountingPeriod command)
    {
        if (!TryGetUnlockedFiscalYear(command.FiscalYearId.Value, out var fiscalYear))
            return;

        if (!TryGetUnlockedAccountingPeriod(fiscalYear, command.AccountingPeriodId.Value))
            return;

        if (!TryValidateAccountingPeriodDeletion(command.AccountingPeriodId.Value, fiscalYear))
            return;

        PersistDeletedAccountingPeriod(command, fiscalYear);
        DeferAsync(true, _ => TrySaveSnapshot());
    }

    // Operation: resolves a fiscal year from state, rejecting if locked or missing.
    private bool TryGetUnlockedFiscalYear(string fiscalYearId, out FiscalYearState fiscalYear)
    {
        if (!_state.FiscalYears.TryGetValue(fiscalYearId, out fiscalYear!))
        {
            Sender.Tell("Fiscal year not found.", Self);
            return false;
        }

        if (fiscalYear.IsLocked)
        {
            Sender.Tell("Locked fiscal years cannot be modified.", Self);
            return false;
        }

        return true;
    }

    // Operation: resolves a period from a fiscal year, rejecting if locked or missing.
    private bool TryGetUnlockedAccountingPeriod(FiscalYearState fiscalYear, string periodId)
    {
        if (!fiscalYear.Periods.TryGetValue(periodId, out var period))
        {
            Sender.Tell("Accounting period not found.", Self);
            return false;
        }

        if (period.IsLocked)
        {
            Sender.Tell("Locked fiscal years or periods cannot be modified.", Self);
            return false;
        }

        return true;
    }

    // Operation: validates that adding a new period preserves full, contiguous fiscal-year coverage.
    private bool TryValidateAccountingPeriodCreation(CreateAccountingPeriod command, FiscalYearState fiscalYear)
    {
        if (fiscalYear.Periods.ContainsKey(command.AccountingPeriodId.Value))
        {
            Sender.Tell("Accounting period already exists.", Self);
            return false;
        }

        var candidatePeriods = fiscalYear.Periods.Values
            .Select(ToDateRange)
            .Append(new DateRange(command.StartDate.Date, command.EndDate.Date))
            .ToList();

        if (CoversWholeFiscalYear(fiscalYear, candidatePeriods))
            return true;

        Sender.Tell("Accounting periods must stay contiguous and fully cover the fiscal year.", Self);
        return false;
    }

    // Operation: validates that updating a period preserves full, contiguous fiscal-year coverage.
    private bool TryValidateAccountingPeriodUpdate(UpdateAccountingPeriod command, FiscalYearState fiscalYear)
    {
        var candidatePeriods = fiscalYear.Periods.Values
            .Where(period => period.Id != command.AccountingPeriodId.Value)
            .Select(ToDateRange)
            .Append(new DateRange(command.StartDate.Date, command.EndDate.Date))
            .ToList();

        if (CoversWholeFiscalYear(fiscalYear, candidatePeriods))
            return true;

        Sender.Tell("Accounting periods must stay contiguous and fully cover the fiscal year.", Self);
        return false;
    }

    // Operation: validates that removing a period still leaves the fiscal year fully covered.
    private bool TryValidateAccountingPeriodDeletion(string periodId, FiscalYearState fiscalYear)
    {
        var candidatePeriods = fiscalYear.Periods.Values
            .Where(current => current.Id != periodId)
            .Select(ToDateRange)
            .ToList();

        if (CoversWholeFiscalYear(fiscalYear, candidatePeriods))
            return true;

        Sender.Tell("Deleting this period would break full fiscal-year coverage.", Self);
        return false;
    }

    // Integration: persists a new accounting period and syncs the fiscal-year lock state.
    private void PersistCreatedAccountingPeriod(CreateAccountingPeriod command, FiscalYearState fiscalYear)
    {
        var created = new AccountingPeriodCreated(
            command.OrganisationId,
            command.FiscalYearId,
            command.AccountingPeriodId,
            command.SequenceNumber,
            command.StartDate.Date,
            command.EndDate.Date,
            command.IsLocked);

        Persist(created, Apply);
        PersistFiscalYearLockSync(command.OrganisationId, fiscalYear.Id);
    }

    // Integration: persists an updated accounting period and syncs the fiscal-year lock state.
    private void PersistUpdatedAccountingPeriod(UpdateAccountingPeriod command, FiscalYearState fiscalYear)
    {
        var updated = new AccountingPeriodUpdated(
            command.OrganisationId,
            command.FiscalYearId,
            command.AccountingPeriodId,
            command.SequenceNumber,
            command.StartDate.Date,
            command.EndDate.Date,
            command.IsLocked);

        Persist(updated, Apply);
        PersistFiscalYearLockSync(command.OrganisationId, fiscalYear.Id);
    }

    // Integration: persists accounting period deletion and syncs the fiscal-year lock state.
    private void PersistDeletedAccountingPeriod(DeleteAccountingPeriod command, FiscalYearState fiscalYear)
    {
        Persist(new AccountingPeriodDeleted(command.OrganisationId, command.FiscalYearId, command.AccountingPeriodId), Apply);
        PersistFiscalYearLockSync(command.OrganisationId, fiscalYear.Id);
    }

    // Operation: synchronize fiscal-year lock from its period states.
    private void PersistFiscalYearLockSync(OrganisationId organisationId, string fiscalYearId)
    {
        if (!_state.FiscalYears.TryGetValue(fiscalYearId, out var fiscalYear))
        {
            return;
        }

        if (fiscalYear.Periods.Count == 0)
        {
            return;
        }

        var allPeriodsClosed = fiscalYear.Periods.Values.All(period => period.IsLocked);
        if (allPeriodsClosed && !fiscalYear.IsLocked)
        {
            var closeFiscalYear = new FiscalYearUpdated(
                organisationId,
                FiscalYearId.With(fiscalYear.Id),
                fiscalYear.StartDate,
                fiscalYear.EndDate,
                true);
            Persist(closeFiscalYear, Apply);
            return;
        }

        var anyOpenPeriod = fiscalYear.Periods.Values.Any(period => !period.IsLocked);
        if (anyOpenPeriod && fiscalYear.IsLocked)
        {
            var reopenFiscalYear = new FiscalYearUpdated(
                organisationId,
                FiscalYearId.With(fiscalYear.Id),
                fiscalYear.StartDate,
                fiscalYear.EndDate,
                false);
            Persist(reopenFiscalYear, Apply);
        }
    }

    private void Apply(FiscalYearCreated @event)
    {
        var fiscalYear = new FiscalYearState
        {
            Id = @event.FiscalYearId.Value,
            StartDate = @event.StartDate.Date,
            EndDate = @event.EndDate.Date,
            IsLocked = @event.IsLocked
        };

        _state.FiscalYears[@event.FiscalYearId.Value] = fiscalYear;
    }

    private void Apply(FiscalYearUpdated @event)
    {
        if (!_state.FiscalYears.TryGetValue(@event.FiscalYearId.Value, out var fiscalYear))
        {
            return;
        }

        fiscalYear.StartDate = @event.StartDate.Date;
        fiscalYear.EndDate = @event.EndDate.Date;
        fiscalYear.IsLocked = @event.IsLocked;

        if (fiscalYear.IsLocked)
        {
            foreach (var period in fiscalYear.Periods.Values)
            {
                period.IsLocked = true;
            }
        }
    }

    private void Apply(FiscalYearDeleted @event)
    {
        _state.FiscalYears.Remove(@event.FiscalYearId.Value);
    }

    private void Apply(AccountingPeriodCreated @event)
    {
        if (!_state.FiscalYears.TryGetValue(@event.FiscalYearId.Value, out var fiscalYear))
        {
            return;
        }  

        fiscalYear.Periods[@event.AccountingPeriodId.Value] = new AccountingPeriodState
        {
            Id = @event.AccountingPeriodId.Value,
            SequenceNumber = @event.SequenceNumber,
            StartDate = @event.StartDate.Date,
            EndDate = @event.EndDate.Date,
            IsLocked = fiscalYear.IsLocked || @event.IsLocked
        };
    }

    private void Apply(AccountingPeriodUpdated @event)
    {
        if (!_state.FiscalYears.TryGetValue(@event.FiscalYearId.Value, out var fiscalYear))
        {
            return;
        }

        if (!fiscalYear.Periods.TryGetValue(@event.AccountingPeriodId.Value, out var period))
        {
            return;
        }

        period.SequenceNumber = @event.SequenceNumber;
        period.StartDate = @event.StartDate.Date;
        period.EndDate = @event.EndDate.Date;
        period.IsLocked = fiscalYear.IsLocked || @event.IsLocked;
    }

    private void Apply(AccountingPeriodDeleted @event)
    {
        if (!_state.FiscalYears.TryGetValue(@event.FiscalYearId.Value, out var fiscalYear))
        {
            return;
        }

        fiscalYear.Periods.Remove(@event.AccountingPeriodId.Value);
    }

    private void Apply(FiscalYearPeriodsRegenerated @event)
    {
        if (!_state.FiscalYears.TryGetValue(@event.FiscalYearId.Value, out var fiscalYear))
        {
            return;
        }

        // Remove deleted periods
        foreach (var deletedPeriodId in @event.DeletedPeriodIds)
        {
            fiscalYear.Periods.Remove(deletedPeriodId);
        }

        // Add created periods
        foreach (var createdPeriod in @event.CreatedPeriods)
        {
            fiscalYear.Periods[createdPeriod.Id] = new AccountingPeriodState
            {
                Id = createdPeriod.Id,
                SequenceNumber = createdPeriod.SequenceNumber,
                StartDate = createdPeriod.StartDate,
                EndDate = createdPeriod.EndDate,
                IsLocked = createdPeriod.IsLocked
            };
        }
    }

    // Operation: generate contiguous monthly periods where only the last period can be shorter than a month.
    private static IEnumerable<AccountingPeriodCreated> GenerateDefaultPeriods(
        OrganisationId organisationId,
        FiscalYearId fiscalYearId,
        DateTime startDate,
        DateTime endDate,
        bool isLocked)
    {
        var currentStartDate = startDate.Date;
        var sequenceNumber = 1;

        while (currentStartDate <= endDate.Date)
        {
            var currentEndDate = currentStartDate.AddMonths(1).AddDays(-1);
            if (currentEndDate > endDate.Date)
            {
                currentEndDate = endDate.Date;
            }

            yield return new AccountingPeriodCreated(
                organisationId,
                fiscalYearId,
                AccountingPeriodId.New(),
                sequenceNumber,
                currentStartDate,
                currentEndDate,
                isLocked);

            currentStartDate = currentEndDate.AddDays(1);
            sequenceNumber++;
        }
    }

    private static DateRange ToDateRange(FiscalYearState fiscalYear)
    {
        return new DateRange(fiscalYear.StartDate, fiscalYear.EndDate);
    }

    private static DateRange ToDateRange(AccountingPeriodState period)
    {
        return new DateRange(period.StartDate, period.EndDate);
    }

    private static bool CoversWholeFiscalYear(FiscalYearState fiscalYear, List<DateRange> periods)
    {
        if (!IsContiguous(periods))
        {
            return false;
        }

        if (periods.Count == 0)
        {
            return false;
        }

        var ordered = periods.OrderBy(period => period.StartDate).ToList();
        return ordered[0].StartDate == fiscalYear.StartDate.Date && ordered[^1].EndDate == fiscalYear.EndDate.Date;
    }

    private static bool IsContiguous(List<DateRange> ranges)
    {
        if (ranges.Count <= 1)
        {
            return true;
        }

        var ordered = ranges
            .OrderBy(range => range.StartDate)
            .ToList();

        if (ordered.Any(range => range.EndDate < range.StartDate))
        {
            return false;
        }

        for (var index = 1; index < ordered.Count; index++)
        {
            var previous = ordered[index - 1];
            var current = ordered[index];

            if (current.StartDate != previous.EndDate.AddDays(1))
            {
                return false;
            }
        }

        return true;
    }

    private void TrySaveSnapshot()
    {
        if (LastSequenceNr % 20 == 0)
        {
            SaveSnapshot(_state.Clone());
        }
    }

    private sealed class FiscalCalendarState
    {
        public Dictionary<string, FiscalYearState> FiscalYears { get; } = new();

        public FiscalCalendarState Clone()
        {
            var clone = new FiscalCalendarState();
            foreach (var fiscalYear in FiscalYears)
            {
                clone.FiscalYears[fiscalYear.Key] = fiscalYear.Value.Clone();
            }

            return clone;
        }

        public void CopyFrom(FiscalCalendarState state)
        {
            FiscalYears.Clear();
            foreach (var fiscalYear in state.FiscalYears)
            {
                FiscalYears[fiscalYear.Key] = fiscalYear.Value.Clone();
            }
        }
    }

    private sealed class FiscalYearState
    {
        public string Id { get; init; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsLocked { get; set; }
        public Dictionary<string, AccountingPeriodState> Periods { get; } = new();

        public FiscalYearState Clone()
        {
            var clone = new FiscalYearState
            {
                Id = Id,
                StartDate = StartDate,
                EndDate = EndDate,
                IsLocked = IsLocked
            };

            foreach (var period in Periods)
            {
                clone.Periods[period.Key] = period.Value.Clone();
            }

            return clone;
        }
    }

    private sealed class AccountingPeriodState
    {
        public string Id { get; init; } = string.Empty;
        public int SequenceNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsLocked { get; set; }

        public AccountingPeriodState Clone()
        {
            return new AccountingPeriodState
            {
                Id = Id,
                SequenceNumber = SequenceNumber,
                StartDate = StartDate,
                EndDate = EndDate,
                IsLocked = IsLocked
            };
        }
    }

    private sealed record DateRange(DateTime StartDate, DateTime EndDate);
}



