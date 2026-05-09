using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Notifications;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class FiscalCalendarAndPeriodsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel,
    IDisposable
{
    private static readonly ReadOnlyObservableCollection<FiscalYearEditorItemViewModel> EmptyFiscalYears =
        new(new ObservableCollection<FiscalYearEditorItemViewModel>());

    private static readonly ReadOnlyObservableCollection<AccountingPeriodItemViewModel> EmptyPeriods =
        new(new ObservableCollection<AccountingPeriodItemViewModel>());

    private readonly HashSet<Guid> _deletedFiscalYearIds = [];
    private readonly IFinanceManagementFacade _financeManagementFacade;

    private readonly SourceCache<FiscalYearEditorItemViewModel, Guid> _fiscalYearsSource =
        new(fiscalYear => fiscalYear.Id);

    private readonly IDisposable _fiscalYearsSubscription;
    private readonly CompositeDisposable _fiscalYearSubscriptions = [];
    private readonly INotificationService _notificationService;
    private readonly string _organisationId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private DateTime _draftEndDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private bool _draftIsLocked;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddPeriodCommand))]
    private DateTime _draftNewPeriodEndDate;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmAddPeriodCommand))]
    private DateTime _draftNewPeriodStartDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private DateTime _draftStartDate;

    [ObservableProperty]
    private ReadOnlyObservableCollection<FiscalYearEditorItemViewModel> _fiscalYears = EmptyFiscalYears;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BeginAddPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelAddPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmAddPeriodCommand))]
    private bool _isAddingPeriod;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegeneratePeriodsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseSelectedPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReopenSelectedPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginAddPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmAddPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedPeriodCommand))]
    private bool _isOperationInFlight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFiscalYear))]
    [NotifyPropertyChangedFor(nameof(SelectedFiscalYearNameDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedFiscalYearTimelineDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedFiscalYearStatusDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedFiscalYearPeriodCountDisplay))]
    [NotifyPropertyChangedFor(nameof(EditorModeTitle))]
    [NotifyPropertyChangedFor(nameof(EditorHelpText))]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegeneratePeriodsCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginAddPeriodCommand))]
    private FiscalYearEditorItemViewModel? _selectedFiscalYear;

    private FiscalYearDraftSnapshot? _selectedFiscalYearSnapshot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPeriod))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodNameDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodCoverageDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodDateRangeDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodDurationDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodStatusDisplay))]
    [NotifyCanExecuteChangedFor(nameof(CloseSelectedPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReopenSelectedPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedPeriodCommand))]
    private AccountingPeriodItemViewModel? _selectedPeriod;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ReadOnlyObservableCollection<AccountingPeriodItemViewModel> _visiblePeriods = EmptyPeriods;

    public FiscalCalendarAndPeriodsViewModel(
        IFinanceManagementFacade financeManagementFacade,
        IUserSessionService userSessionService,
        INotificationService notificationService)
    {
        var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _financeManagementFacade = financeManagementFacade;
        _notificationService = notificationService;
        _organisationId = ResolveOrganisationId(userSessionService);

        _fiscalYearsSubscription = _fiscalYearsSource.Connect()
            .AutoRefresh(static fiscalYear => fiscalYear.StartDate)
            .SortAndBind(
                out var fiscalYearsBound,
                SortExpressionComparer<FiscalYearEditorItemViewModel>.Descending(static fiscalYear =>
                    fiscalYear.StartDate))
            .Subscribe();

        FiscalYears = fiscalYearsBound;

        _fiscalYearSubscriptions.Add(_financeManagementFacade.CurrentFiscalYears
            .Where(fiscalYear => fiscalYear.OrganisationId == _organisationId)
            .ObserveOn(syncContext)
            .Subscribe(ApplyCurrentFiscalYear, HandleFiscalYearsStreamError));

        _fiscalYearSubscriptions.Add(_financeManagementFacade.FiscalYearUpdated
            .Where(fiscalYear => fiscalYear.OrganisationId == _organisationId)
            .ObserveOn(syncContext)
            .Subscribe(ApplyUpdatedFiscalYear, HandleFiscalYearsStreamError));

        _fiscalYearSubscriptions.Add(_financeManagementFacade.FiscalYearDeleted
            .Where(fiscalYear => fiscalYear.OrganisationId == _organisationId)
            .ObserveOn(syncContext)
            .Subscribe(ApplyDeletedFiscalYear, HandleFiscalYearsStreamError));
    }

    public bool HasSelectedFiscalYear => SelectedFiscalYear is not null;

    public bool HasSelectedPeriod => SelectedPeriod is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string SelectedFiscalYearNameDisplay => SelectedFiscalYear?.Name ?? "No fiscal year selected";

    public string SelectedFiscalYearTimelineDisplay => SelectedFiscalYear?.DateRangeLabel ??
                                                       "Choose a fiscal year to inspect and edit its generated accounting periods.";

    public string SelectedFiscalYearStatusDisplay => SelectedFiscalYear?.StatusLabel ?? "—";

    public string SelectedFiscalYearPeriodCountDisplay => SelectedFiscalYear is null
        ? "0 periods"
        : $"{SelectedFiscalYear.Periods.Count} generated periods";

    public string TotalFiscalYearsDisplay => FiscalYears.Count == 1
        ? "1 fiscal year"
        : $"{FiscalYears.Count} fiscal years";

    public string LockedFiscalYearsDisplay => FiscalYears.Count(static fiscalYear => fiscalYear.IsLocked) == 1
        ? "1 locked year"
        : $"{FiscalYears.Count(static fiscalYear => fiscalYear.IsLocked)} locked years";

    public string OpenFiscalYearsDisplay => FiscalYears.Count(static fiscalYear => !fiscalYear.IsLocked) == 1
        ? "1 open year"
        : $"{FiscalYears.Count(static fiscalYear => !fiscalYear.IsLocked)} open years";

    public string EditorModeTitle =>
        SelectedFiscalYear is null ? "Select a fiscal year" : $"Editing {SelectedFiscalYear.Name}";

    public string EditorHelpText => SelectedFiscalYear is null
        ? "Pick a fiscal year from the left pane to review the year details and its generated accounting periods."
        : "Adjust the date range or locked status, then save. Accounting periods remain aligned to the selected fiscal year.";

    public string SelectedPeriodNameDisplay => SelectedPeriod?.Name ?? "Select an accounting period";

    public string SelectedPeriodCoverageDisplay => SelectedPeriod?.CoverageLabel ??
                                                   "Choose one of the generated periods to inspect its coverage.";

    public string SelectedPeriodDateRangeDisplay => SelectedPeriod?.DateRangeLabel ?? "—";

    public string SelectedPeriodDurationDisplay => SelectedPeriod?.DurationLabel ?? "—";

    public string SelectedPeriodStatusDisplay => SelectedPeriod?.StatusLabel ?? "—";

    public bool CanToggleFiscalYearLock => !IsOperationInFlight && SelectedFiscalYear is { IsLocked: false };

    public bool CanToggleSelectedPeriodLock => !IsOperationInFlight && SelectedPeriod is { IsLocked: false };

    public bool SelectedPeriodIsLocked
    {
        get => SelectedPeriod?.IsLocked ?? false;
        set
        {
            // Unlocking is intentionally blocked for now; future authorization can enable it.
            if (!value || SelectedPeriod is null || SelectedPeriod.IsLocked) return;

            if (CloseSelectedPeriodCommand.CanExecute(null)) CloseSelectedPeriodCommand.Execute(null);
        }
    }

    public Guid Id => Guid.Parse("76a6a087-42cf-4495-8d6f-48ec84f917da");
    public string Title => "Fiscal Calendar & Periods";
    public object? HeaderContent => null;

    public void Dispose()
    {
        _fiscalYearSubscriptions.Dispose();
        _fiscalYearsSubscription.Dispose();
        _fiscalYearsSource.Dispose();
    }

    // Operation: resolves the current organisation id from the active user session
    private static string ResolveOrganisationId(IUserSessionService userSessionService)
    {
        return userSessionService.CurrentUserSession?.Organisation?.Id
               ?? throw new InvalidOperationException(
                   "An active session with an organisation is required to manage fiscal calendars.");
    }

    partial void OnSelectedFiscalYearChanged(FiscalYearEditorItemViewModel? value)
    {
        LoadDraftFromSelection(value);
        VisiblePeriods = value?.Periods ?? EmptyPeriods;
        SelectedPeriod = VisiblePeriods.FirstOrDefault();
        IsAddingPeriod = false;
        OnPropertyChanged(nameof(CanToggleFiscalYearLock));
    }

    partial void OnSelectedPeriodChanged(AccountingPeriodItemViewModel? value)
    {
        _ = value;
        OnPropertyChanged(nameof(CanToggleSelectedPeriodLock));
        OnPropertyChanged(nameof(SelectedPeriodIsLocked));
        RefreshPeriodCommandState();
    }

    [RelayCommand(CanExecute = nameof(CanAddFiscalYear))]
    private async Task AddFiscalYear()
    {
        var (startDate, endDate) = GetNextAvailableFiscalYearRange();
        var subject = $"{startDate:yyyy}-{endDate:yy}";

        var isConfirmed = await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.CreateFiscalYear(_organisationId, startDate, endDate, false),
            _financeManagementFacade.CurrentFiscalYears,
            fiscalYear =>
                fiscalYear.OrganisationId == _organisationId &&
                fiscalYear.StartDate.Date == startDate.Date &&
                fiscalYear.EndDate.Date == endDate.Date,
            TimeSpan.FromSeconds(10),
            "Fiscal year",
            "Created",
            subject,
            SetOperationInFlight);

        if (!isConfirmed) return;

        try
        {
            var createdFiscalYearId =
                await WaitForCreatedFiscalYearWithPeriodsAsync(startDate, endDate, TimeSpan.FromSeconds(3));
            await ReloadFiscalYearsFromReadModelAsync(createdFiscalYearId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Created fiscal year, but refresh is still catching up: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFiscalYear))]
    private async Task SaveFiscalYear()
    {
        if (SelectedFiscalYear is null) return;

        if (DraftEndDate.Date < DraftStartDate.Date)
        {
            StatusMessage = "The fiscal year end date must be on or after the start date.";
            return;
        }

        // Capture before the await — SelectedFiscalYear can be nulled by re-entrant
        // UI updates (DynamicData re-sort on StartDate change) during the async gap.
        var fiscalYear = SelectedFiscalYear;
        var draftStart = DraftStartDate;
        var draftEnd = DraftEndDate;
        var draftLocked = DraftIsLocked;

        var fiscalYearId = fiscalYear.Id.ToString();

        await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.UpdateFiscalYear(_organisationId, fiscalYearId, draftStart, draftEnd,
                draftLocked),
            _financeManagementFacade.FiscalYearUpdated,
            updated =>
                updated.OrganisationId == _organisationId &&
                updated.Id == fiscalYearId &&
                updated.StartDate.Date == draftStart.Date &&
                updated.EndDate.Date == draftEnd.Date &&
                updated.IsLocked == draftLocked,
            TimeSpan.FromSeconds(10),
            "Fiscal year",
            "Saved",
            fiscalYear.Name,
            SetOperationInFlight);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteFiscalYear))]
    private async Task DeleteFiscalYear()
    {
        if (SelectedFiscalYear is null) return;

        if (FiscalYears.Count == 1)
        {
            StatusMessage = "Keep at least one fiscal year in the timeline.";
            return;
        }

        if (!CanDeleteSelectedFiscalYearWithoutCreatingGap())
        {
            const string message =
                "Deleting this fiscal year would create a gap. Adjust neighboring fiscal years first.";
            StatusMessage = message;
            _notificationService.Show("Cannot delete fiscal year", message, NotificationType.Warning);
            return;
        }

        var deletedFiscalYear = SelectedFiscalYear;
        var deletedFiscalYearId = deletedFiscalYear.Id.ToString();

        var isConfirmed = await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.DeleteFiscalYear(_organisationId, deletedFiscalYearId),
            BuildFiscalYearDeletionConfirmationObservable(deletedFiscalYearId),
            TimeSpan.FromSeconds(10),
            "Fiscal year",
            "Deleted",
            deletedFiscalYear.Name,
            SetOperationInFlight);

        if (isConfirmed)
        {
            RemoveFiscalYearFromUiCache(deletedFiscalYearId);

            try
            {
                await ReloadFiscalYearsFromReadModelAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Deleted fiscal year, but refresh is still catching up: {ex.Message}";
            }

            return;
        }

        // Fallback: deletion can complete in the read model after a timeout notification.
        var wasDeletedInReadModel = await WaitForFiscalYearAbsenceAsync(deletedFiscalYearId, TimeSpan.FromSeconds(2));
        if (wasDeletedInReadModel)
        {
            try
            {
                RemoveFiscalYearFromUiCache(deletedFiscalYearId);
                await ReloadFiscalYearsFromReadModelAsync();
                _notificationService.Show("Fiscal year", $"Deleted {deletedFiscalYear.Name} successfully.",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Deleted fiscal year, but refresh is still catching up: {ex.Message}";
            }

            return;
        }

        // Integration: if stream confirmation was missed, reconcile against the authoritative read model before exiting.
        var reconciled =
            await TryReconcileDeletedFiscalYearFromReadModelAsync(deletedFiscalYearId, deletedFiscalYear.Name);
        if (!reconciled)
            StatusMessage =
                $"Delete command sent for {deletedFiscalYear.Name}, but UI is still waiting for projection confirmation.";
    }

    // Operation: reloads read-model state and confirms whether the target fiscal year is already gone.
    private async Task<bool> TryReconcileDeletedFiscalYearFromReadModelAsync(string fiscalYearId, string fiscalYearName)
    {
        try
        {
            await ReloadFiscalYearsFromReadModelAsync();

            var isStillPresent = FiscalYears.Any(fiscalYear => fiscalYear.Id.ToString() == fiscalYearId);
            if (isStillPresent) return false;

            _notificationService.Show("Fiscal year", $"Deleted {fiscalYearName} successfully.",
                NotificationType.Success);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Deleted fiscal year, but refresh is still catching up: {ex.Message}";
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRevertFiscalYear))]
    private void RevertFiscalYear()
    {
        if (_selectedFiscalYearSnapshot is null) return;

        DraftStartDate = _selectedFiscalYearSnapshot.StartDate;
        DraftEndDate = _selectedFiscalYearSnapshot.EndDate;
        DraftIsLocked = _selectedFiscalYearSnapshot.IsLocked;
        StatusMessage = $"Reverted the editor back to {_selectedFiscalYearSnapshot.Name}.";
    }

    [RelayCommand(CanExecute = nameof(CanRegeneratePeriods))]
    private Task RegeneratePeriods()
    {
        if (SelectedFiscalYear is null) return Task.CompletedTask;

        SelectedFiscalYear.ReplacePeriods(GenerateAccountingPeriods(SelectedFiscalYear.StartDate,
            SelectedFiscalYear.EndDate, SelectedFiscalYear.IsLocked));
        SynchronizeFiscalYearAndPeriodLockState(SelectedFiscalYear);
        SelectedPeriod = SelectedFiscalYear.Periods.FirstOrDefault();
        StatusMessage =
            $"Regenerated {SelectedFiscalYear.Periods.Count} accounting periods for {SelectedFiscalYear.Name}.";
        RaiseSelectionStateChanged();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanCloseSelectedPeriod))]
    private async Task CloseSelectedPeriod()
    {
        if (SelectedFiscalYear is null || SelectedPeriod is null) return;

        var fiscalYearId = SelectedFiscalYear.Id.ToString();
        var periodId = SelectedPeriod.Id;
        var periodName = SelectedPeriod.Name;
        var sequenceNumber = SelectedPeriod.SequenceNumber;
        var startDate = SelectedPeriod.StartDate;
        var endDate = SelectedPeriod.EndDate;

        await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.UpdateAccountingPeriod(
                _organisationId,
                fiscalYearId,
                periodId,
                sequenceNumber,
                startDate,
                endDate,
                true),
            _financeManagementFacade.FiscalYearUpdated,
            updated =>
                updated.OrganisationId == _organisationId &&
                updated.Id == fiscalYearId &&
                updated.Periods.Any(period => period.Id == periodId && period.IsLocked),
            TimeSpan.FromSeconds(10),
            "Accounting period",
            "Closed",
            periodName,
            SetOperationInFlight);
    }

    [RelayCommand(CanExecute = nameof(CanReopenSelectedPeriod))]
    private async Task ReopenSelectedPeriod()
    {
        if (SelectedFiscalYear is null || SelectedPeriod is null) return;

        var fiscalYearId = SelectedFiscalYear.Id.ToString();
        var periodId = SelectedPeriod.Id;
        var periodName = SelectedPeriod.Name;
        var sequenceNumber = SelectedPeriod.SequenceNumber;
        var startDate = SelectedPeriod.StartDate;
        var endDate = SelectedPeriod.EndDate;

        await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.UpdateAccountingPeriod(
                _organisationId,
                fiscalYearId,
                periodId,
                sequenceNumber,
                startDate,
                endDate,
                false),
            _financeManagementFacade.FiscalYearUpdated,
            updated =>
                updated.OrganisationId == _organisationId &&
                updated.Id == fiscalYearId &&
                updated.Periods.Any(period => period.Id == periodId && !period.IsLocked),
            TimeSpan.FromSeconds(10),
            "Accounting period",
            "Reopened",
            periodName,
            SetOperationInFlight);
    }

    [RelayCommand(CanExecute = nameof(CanBeginAddPeriod))]
    private void BeginAddPeriod()
    {
        var (startDate, endDate) = ComputeNextPeriodGap();
        DraftNewPeriodStartDate = startDate;
        DraftNewPeriodEndDate = endDate;
        IsAddingPeriod = true;
    }

    [RelayCommand(CanExecute = nameof(CanCancelAddPeriod))]
    private void CancelAddPeriod()
    {
        IsAddingPeriod = false;
    }

    // Integration: validates draft dates, dispatches CreateAccountingPeriod, and waits for read-model confirmation.
    [RelayCommand(CanExecute = nameof(CanConfirmAddPeriod))]
    private async Task ConfirmAddPeriod()
    {
        if (SelectedFiscalYear is null) return;

        if (DraftNewPeriodEndDate.Date < DraftNewPeriodStartDate.Date)
        {
            StatusMessage = "The period end date must be on or after the start date.";
            return;
        }

        var fiscalYearId = SelectedFiscalYear.Id.ToString();
        var startDate = DraftNewPeriodStartDate;
        var endDate = DraftNewPeriodEndDate;
        var sequenceNumber = ComputeSequenceNumberForNewPeriod(startDate);
        var subject = $"{startDate:dd MMM} – {endDate:dd MMM yyyy}";

        var isConfirmed = await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.CreateAccountingPeriod(
                _organisationId, fiscalYearId, sequenceNumber, startDate, endDate, false),
            _financeManagementFacade.FiscalYearUpdated,
            updated =>
                updated.OrganisationId == _organisationId &&
                updated.Id == fiscalYearId &&
                updated.Periods.Any(period =>
                    period.StartDate.Date == startDate.Date &&
                    period.EndDate.Date == endDate.Date),
            TimeSpan.FromSeconds(10),
            "Accounting period",
            "Created",
            subject,
            SetOperationInFlight);

        if (isConfirmed) IsAddingPeriod = false;
    }

    // Integration: dispatches DeleteAccountingPeriod for the selected period and awaits read-model confirmation.
    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPeriod))]
    private async Task DeleteSelectedPeriod()
    {
        if (SelectedFiscalYear is null || SelectedPeriod is null) return;

        var fiscalYearId = SelectedFiscalYear.Id.ToString();
        var periodId = SelectedPeriod.Id;
        var periodName = SelectedPeriod.Name;

        await _notificationService.TrackOperationAsync(
            () => _financeManagementFacade.DeleteAccountingPeriod(
                _organisationId, fiscalYearId, periodId),
            _financeManagementFacade.FiscalYearUpdated,
            updated =>
                updated.OrganisationId == _organisationId &&
                updated.Id == fiscalYearId &&
                updated.Periods.All(period => period.Id != periodId),
            TimeSpan.FromSeconds(10),
            "Accounting period",
            "Deleted",
            periodName,
            SetOperationInFlight);
    }

    private bool CanBeginAddPeriod()
    {
        return !IsOperationInFlight && !IsAddingPeriod && SelectedFiscalYear is { IsLocked: false };
    }

    private bool CanCancelAddPeriod()
    {
        return IsAddingPeriod;
    }

    private bool CanConfirmAddPeriod()
    {
        return !IsOperationInFlight &&
               IsAddingPeriod &&
               SelectedFiscalYear is not null &&
               DraftNewPeriodEndDate.Date >= DraftNewPeriodStartDate.Date;
    }

    private bool CanDeleteSelectedPeriod()
    {
        return !IsOperationInFlight &&
               SelectedPeriod is { IsLocked: false } &&
               SelectedFiscalYear is { IsLocked: false };
    }

    // Operation: finds the first uncovered date range within the selected fiscal year relative to its existing periods.
    private (DateTime StartDate, DateTime EndDate) ComputeNextPeriodGap()
    {
        if (SelectedFiscalYear is null) return (DateTime.Today, DateTime.Today.AddMonths(1).AddDays(-1));

        var fiscalStart = SelectedFiscalYear.StartDate.Date;
        var fiscalEnd = SelectedFiscalYear.EndDate.Date;
        var orderedPeriods = VisiblePeriods.OrderBy(static period => period.StartDate).ToList();

        var expectedStart = fiscalStart;
        foreach (var period in orderedPeriods)
        {
            if (period.StartDate.Date > expectedStart) return (expectedStart, period.StartDate.Date.AddDays(-1));

            if (period.EndDate.Date >= expectedStart) expectedStart = period.EndDate.Date.AddDays(1);
        }

        return expectedStart <= fiscalEnd
            ? (expectedStart, fiscalEnd)
            : (fiscalStart, fiscalEnd);
    }

    // Operation: computes the 1-based sequence number for a period starting at the given date.
    private int ComputeSequenceNumberForNewPeriod(DateTime startDate)
    {
        var periodsBeforeNew = VisiblePeriods
            .Count(period => period.StartDate.Date < startDate.Date);
        return periodsBeforeNew + 1;
    }

    private bool CanAddFiscalYear()
    {
        return !IsOperationInFlight;
    }

    private bool CanSaveFiscalYear()
    {
        return !IsOperationInFlight &&
               SelectedFiscalYear is not null &&
               DraftEndDate.Date >= DraftStartDate.Date;
    }

    private bool CanDeleteFiscalYear()
    {
        return !IsOperationInFlight &&
               SelectedFiscalYear is not null &&
               FiscalYears.Count > 1 &&
               FiscalYears.Any(fiscalYear => fiscalYear.Id == SelectedFiscalYear.Id) &&
               CanDeleteSelectedFiscalYearWithoutCreatingGap();
    }

    // Operation: validates that deleting the selected fiscal year keeps the remaining timeline contiguous.
    private bool CanDeleteSelectedFiscalYearWithoutCreatingGap()
    {
        if (SelectedFiscalYear is null) return false;

        var remainingRanges = FiscalYears
            .Where(fiscalYear => fiscalYear.Id != SelectedFiscalYear.Id)
            .Select(fiscalYear => new FiscalYearRange(fiscalYear.StartDate.Date, fiscalYear.EndDate.Date))
            .ToList();

        return remainingRanges.Count <= 1 || IsContiguous(remainingRanges);
    }

    // Operation: confirms deletion either from the projected delete stream or from the fiscal year disappearing from the read model.
    private IObservable<bool> BuildFiscalYearDeletionConfirmationObservable(string fiscalYearId)
    {
        var deleteEvents = _financeManagementFacade.FiscalYearDeleted
            .Where(deleted => deleted.OrganisationId == _organisationId && deleted.Id == fiscalYearId)
            .Select(_ => true);

        var readModelAbsenceChecks = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .StartWith(0L)
            .SelectMany(_ => Observable.FromAsync(() => HasFiscalYearBeenDeletedAsync(fiscalYearId)))
            .Where(isDeleted => isDeleted)
            .Select(_ => true);

        return deleteEvents
            .Merge(readModelAbsenceChecks)
            .Take(1);
    }

    // Operation: checks whether the fiscal year is no longer present in the projected read model.
    private async Task<bool> HasFiscalYearBeenDeletedAsync(string fiscalYearId)
    {
        try
        {
            var fiscalYears = await _financeManagementFacade.GetFiscalYears(_organisationId);
            return fiscalYears.All(fiscalYear => fiscalYear.Id != fiscalYearId);
        }
        catch
        {
            return false;
        }
    }

    // Operation: retries read-model absence checks for a short window when projector lag occurs.
    private async Task<bool> WaitForFiscalYearAbsenceAsync(string fiscalYearId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow <= deadline)
        {
            if (await HasFiscalYearBeenDeletedAsync(fiscalYearId)) return true;

            await Task.Delay(200);
        }

        return false;
    }

    // Operation: removes one fiscal year from the in-memory editor cache when deletion has been confirmed.
    private void RemoveFiscalYearFromUiCache(string fiscalYearId)
    {
        ApplyDeletedFiscalYear(new FiscalYearData
        {
            Id = fiscalYearId,
            OrganisationId = _organisationId
        });
    }

    // Operation: waits until the created fiscal year is queryable together with its generated periods.
    private async Task<string?> WaitForCreatedFiscalYearWithPeriodsAsync(DateTime startDate, DateTime endDate,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            var fiscalYears = await _financeManagementFacade.GetFiscalYears(_organisationId);
            var createdFiscalYear = fiscalYears.FirstOrDefault(fiscalYear =>
                fiscalYear.StartDate.Date == startDate.Date &&
                fiscalYear.EndDate.Date == endDate.Date);

            if (createdFiscalYear?.Periods.Count > 0) return createdFiscalYear.Id;

            await Task.Delay(200);
        }

        return null;
    }

    // Integration: reloads the fiscal-year editor state from the current read model after a confirmed delete.
    private async Task ReloadFiscalYearsFromReadModelAsync(string? preferredFiscalYearId = null)
    {
        var selectedFiscalYearId = SelectedFiscalYear?.Id;
        var fiscalYears = await _financeManagementFacade.GetFiscalYears(_organisationId);
        var mappedFiscalYears = fiscalYears
            .Select(MapToFiscalYearItem)
            .Where(static fiscalYear => fiscalYear is not null)
            .Select(static fiscalYear => fiscalYear!)
            .ToList();

        var existingFiscalYearIds = mappedFiscalYears
            .Select(fiscalYear => fiscalYear.Id)
            .ToHashSet();
        _deletedFiscalYearIds.RemoveWhere(existingFiscalYearIds.Contains);

        _fiscalYearsSource.Edit(updater =>
        {
            updater.Clear();
            updater.AddOrUpdate(mappedFiscalYears);
        });

        var preferredFiscalYearGuid = Guid.TryParse(preferredFiscalYearId, out var parsedFiscalYearId)
            ? parsedFiscalYearId
            : (Guid?)null;

        SelectedFiscalYear = preferredFiscalYearGuid is not null
            ? FiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id == preferredFiscalYearGuid.Value)
            : selectedFiscalYearId is null
                ? FiscalYears.FirstOrDefault()
                : FiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id == selectedFiscalYearId.Value) ??
                  FiscalYears.FirstOrDefault();

        RaiseCollectionSummaryChanged();
        RaiseSelectionStateChanged();
    }

    // Operation: checks contiguous date coverage (next start must be exactly previous end + 1 day).
    private static bool IsContiguous(IReadOnlyList<FiscalYearRange> ranges)
    {
        var ordered = ranges
            .OrderBy(range => range.StartDate)
            .ToList();

        if (ordered.Any(range => range.EndDate < range.StartDate)) return false;

        for (var index = 1; index < ordered.Count; index++)
            if (ordered[index].StartDate != ordered[index - 1].EndDate.AddDays(1))
                return false;

        return true;
    }

    private bool CanRevertFiscalYear()
    {
        return !IsOperationInFlight &&
               SelectedFiscalYear is not null &&
               _selectedFiscalYearSnapshot is not null;
    }

    private bool CanRegeneratePeriods()
    {
        return !IsOperationInFlight && SelectedFiscalYear is not null;
    }

    private bool CanCloseSelectedPeriod()
    {
        return !IsOperationInFlight && SelectedPeriod is { IsLocked: false };
    }

    private bool CanReopenSelectedPeriod()
    {
        return !IsOperationInFlight && SelectedPeriod is { IsLocked: true };
    }

    private void SetOperationInFlight(bool isInFlight)
    {
        IsOperationInFlight = isInFlight;
        OnPropertyChanged(nameof(CanToggleFiscalYearLock));
        OnPropertyChanged(nameof(CanToggleSelectedPeriodLock));
        OnPropertyChanged(nameof(SelectedPeriodIsLocked));
        BeginAddPeriodCommand.NotifyCanExecuteChanged();
        DeleteSelectedPeriodCommand.NotifyCanExecuteChanged();
    }

    // Integration: applies one current fiscal year from the facade current-state stream.
    private void ApplyCurrentFiscalYear(FiscalYearData fiscalYearData)
    {
        if (!Guid.TryParse(fiscalYearData.Id, out var fiscalYearId)) return;

        // Ignore stale current-state snapshots that were emitted before a confirmed delete.
        if (_deletedFiscalYearIds.Contains(fiscalYearId)) return;

        var existingFiscalYear = _fiscalYearsSource.Lookup(fiscalYearId);
        if (existingFiscalYear.HasValue)
        {
            var selectedPeriodId = SelectedFiscalYear?.Id == fiscalYearId ? SelectedPeriod?.Id : null;
            var incomingPeriods = MapToAccountingPeriods(fiscalYearData);

            existingFiscalYear.Value.StartDate = fiscalYearData.StartDate;
            existingFiscalYear.Value.EndDate = fiscalYearData.EndDate;
            existingFiscalYear.Value.IsLocked = fiscalYearData.IsLocked;
            existingFiscalYear.Value.RefreshName();

            // Keep richer period state when a stale current-state snapshot arrives without periods.
            if (incomingPeriods.Count > 0 || existingFiscalYear.Value.Periods.Count == 0)
                existingFiscalYear.Value.ReplacePeriods(incomingPeriods);

            if (SelectedFiscalYear?.Id == fiscalYearId)
            {
                VisiblePeriods = existingFiscalYear.Value.Periods;
                SelectedPeriod = selectedPeriodId is null
                    ? existingFiscalYear.Value.Periods.FirstOrDefault()
                    : existingFiscalYear.Value.Periods.FirstOrDefault(period => period.Id == selectedPeriodId) ??
                      existingFiscalYear.Value.Periods.FirstOrDefault();
            }

            RaiseSelectionStateChanged();
            RaiseCollectionSummaryChanged();
            return;
        }

        var viewModel = MapToFiscalYearItem(fiscalYearData);
        if (viewModel is null) return;

        _fiscalYearsSource.AddOrUpdate(viewModel);

        if (SelectedFiscalYear is null)
            SelectedFiscalYear = FiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id == viewModel.Id) ??
                                 FiscalYears.FirstOrDefault();

        RaiseCollectionSummaryChanged();
    }

    // Integration: applies one updated fiscal year from the facade update stream.
    private void ApplyUpdatedFiscalYear(FiscalYearData fiscalYearData)
    {
        if (!Guid.TryParse(fiscalYearData.Id, out var fiscalYearId)) return;

        // Ignore late/stale updates that arrive after this fiscal year was confirmed deleted.
        if (_deletedFiscalYearIds.Contains(fiscalYearId)) return;

        var existingFiscalYear = _fiscalYearsSource.Lookup(fiscalYearId);
        if (!existingFiscalYear.HasValue)
        {
            var viewModel = MapToFiscalYearItem(fiscalYearData);
            if (viewModel is null) return;

            _fiscalYearsSource.AddOrUpdate(viewModel);
            RaiseCollectionSummaryChanged();
            return;
        }

        var selectedPeriodId = SelectedFiscalYear?.Id == fiscalYearId ? SelectedPeriod?.Id : null;
        existingFiscalYear.Value.StartDate = fiscalYearData.StartDate;
        existingFiscalYear.Value.EndDate = fiscalYearData.EndDate;
        existingFiscalYear.Value.IsLocked = fiscalYearData.IsLocked;
        existingFiscalYear.Value.RefreshName();
        existingFiscalYear.Value.ReplacePeriods(MapToAccountingPeriods(fiscalYearData));

        if (SelectedFiscalYear?.Id == fiscalYearId)
        {
            VisiblePeriods = existingFiscalYear.Value.Periods;
            SelectedPeriod = selectedPeriodId is null
                ? existingFiscalYear.Value.Periods.FirstOrDefault()
                : existingFiscalYear.Value.Periods.FirstOrDefault(period => period.Id == selectedPeriodId) ??
                  existingFiscalYear.Value.Periods.FirstOrDefault();

            LoadDraftFromSelection(existingFiscalYear.Value);
        }

        RaiseSelectionStateChanged();
        RaiseCollectionSummaryChanged();
    }

    // Integration: applies one deleted fiscal year from the facade delete stream.
    private void ApplyDeletedFiscalYear(FiscalYearData fiscalYearData)
    {
        if (!Guid.TryParse(fiscalYearData.Id, out var fiscalYearId)) return;

        _deletedFiscalYearIds.Add(fiscalYearId);

        var deletedFiscalYear = _fiscalYearsSource.Lookup(fiscalYearId);
        if (!deletedFiscalYear.HasValue) return;

        _fiscalYearsSource.RemoveKey(fiscalYearId);

        // Normalize selection after removal so subsequent delete clicks target an existing row.
        var replacementSelection = FiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id != fiscalYearId);
        var selectedFiscalYearWasRemoved = SelectedFiscalYear?.Id == fiscalYearId;
        var selectedFiscalYearIsStale = SelectedFiscalYear is not null &&
                                        FiscalYears.All(fiscalYear => fiscalYear.Id != SelectedFiscalYear.Id);
        if (selectedFiscalYearWasRemoved || selectedFiscalYearIsStale) SelectedFiscalYear = replacementSelection;

        RaiseSelectionStateChanged();
        RaiseCollectionSummaryChanged();
    }

    // Operation: maps one fiscal-year data object into its view-model representation.
    private static FiscalYearEditorItemViewModel? MapToFiscalYearItem(FiscalYearData fiscalYearData)
    {
        if (!Guid.TryParse(fiscalYearData.Id, out var fiscalYearId)) return null;

        var viewModel = new FiscalYearEditorItemViewModel(
            fiscalYearId,
            fiscalYearData.StartDate,
            fiscalYearData.EndDate,
            fiscalYearData.IsLocked);

        viewModel.ReplacePeriods(MapToAccountingPeriods(fiscalYearData));
        return viewModel;
    }

    // Operation: maps persisted accounting periods into period view models.
    private static IReadOnlyList<AccountingPeriodItemViewModel> MapToAccountingPeriods(FiscalYearData fiscalYearData)
    {
        return fiscalYearData.Periods
            .GroupBy(period => new { StartDate = period.StartDate.Date, EndDate = period.EndDate.Date })
            .Select(group => group.OrderBy(period => period.SequenceNumber).First())
            .OrderBy(period => period.SequenceNumber)
            .Select(period => new AccountingPeriodItemViewModel(period.Id, period.SequenceNumber, period.StartDate,
                period.EndDate, period.IsLocked))
            .ToList();
    }

    // Operation: surfaces stream failures in the status banner.
    private void HandleFiscalYearsStreamError(Exception ex)
    {
        StatusMessage = $"Error subscribing to fiscal years: {ex.Message}";
    }

    private void LoadDraftFromSelection(FiscalYearEditorItemViewModel? fiscalYear)
    {
        if (fiscalYear is null)
        {
            var (startDate, endDate) = GetNextAvailableFiscalYearRange();
            DraftStartDate = startDate;
            DraftEndDate = endDate;
            DraftIsLocked = false;
            _selectedFiscalYearSnapshot = null;
            return;
        }

        DraftStartDate = fiscalYear.StartDate;
        DraftEndDate = fiscalYear.EndDate;
        DraftIsLocked = fiscalYear.IsLocked;
        _selectedFiscalYearSnapshot = new FiscalYearDraftSnapshot(fiscalYear.Name, fiscalYear.StartDate,
            fiscalYear.EndDate, fiscalYear.IsLocked);
    }


    // Operation: computes the next contiguous one-year fiscal span with no gaps.
    private (DateTime StartDate, DateTime EndDate) GetNextAvailableFiscalYearRange()
    {
        var newestFiscalYear = FiscalYears.MaxBy(static fiscalYear => fiscalYear.EndDate);
        var startDate = newestFiscalYear?.EndDate.AddDays(1) ?? new DateTime(DateTime.Today.Year, 1, 1);
        var endDate = startDate.AddYears(1).AddDays(-1);
        return (startDate, endDate);
    }

    private static IReadOnlyList<AccountingPeriodItemViewModel> GenerateAccountingPeriods(DateTime startDate,
        DateTime endDate, bool isLocked)
    {
        var accountingPeriods = new List<AccountingPeriodItemViewModel>();
        var currentStartDate = startDate.Date;
        var sequenceNumber = 1;

        while (currentStartDate <= endDate.Date)
        {
            var currentEndDate = currentStartDate.AddMonths(1).AddDays(-1);

            if (currentEndDate > endDate.Date) currentEndDate = endDate.Date;

            // Generate temporary ID for UI preview; will be replaced when loaded from database
            var tempId = $"temp-period-{Guid.NewGuid():N}";
            accountingPeriods.Add(new AccountingPeriodItemViewModel(tempId, sequenceNumber, currentStartDate,
                currentEndDate, isLocked));
            currentStartDate = currentEndDate.AddDays(1);
            sequenceNumber++;
        }

        return accountingPeriods;
    }


    private static void SynchronizeFiscalYearAndPeriodLockState(FiscalYearEditorItemViewModel fiscalYear)
    {
        if (fiscalYear.IsLocked)
            foreach (var period in fiscalYear.Periods)
                period.IsLocked = true;

        if (AllPeriodsAreClosedAndCoverWholeYear(fiscalYear))
        {
            fiscalYear.IsLocked = true;
            return;
        }

        if (fiscalYear.Periods.Any(static period => !period.IsLocked)) fiscalYear.IsLocked = false;
    }

    private static bool AllPeriodsAreClosedAndCoverWholeYear(FiscalYearEditorItemViewModel fiscalYear)
    {
        if (fiscalYear.Periods.Count == 0 || fiscalYear.Periods.Any(static period => !period.IsLocked)) return false;

        var expectedStartDate = fiscalYear.StartDate.Date;

        foreach (var period in fiscalYear.Periods.OrderBy(static period => period.StartDate))
        {
            if (period.StartDate.Date != expectedStartDate || period.EndDate.Date < period.StartDate.Date) return false;

            expectedStartDate = period.EndDate.Date.AddDays(1);
        }

        return expectedStartDate == fiscalYear.EndDate.Date.AddDays(1);
    }

    private void RaiseCollectionSummaryChanged()
    {
        OnPropertyChanged(nameof(TotalFiscalYearsDisplay));
        OnPropertyChanged(nameof(LockedFiscalYearsDisplay));
        OnPropertyChanged(nameof(OpenFiscalYearsDisplay));
        OnPropertyChanged(nameof(SelectedFiscalYearPeriodCountDisplay));
        DeleteFiscalYearCommand.NotifyCanExecuteChanged();
    }

    private void RaiseSelectionStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedFiscalYear));
        OnPropertyChanged(nameof(CanToggleFiscalYearLock));
        OnPropertyChanged(nameof(SelectedFiscalYearNameDisplay));
        OnPropertyChanged(nameof(SelectedFiscalYearTimelineDisplay));
        OnPropertyChanged(nameof(SelectedFiscalYearStatusDisplay));
        OnPropertyChanged(nameof(SelectedFiscalYearPeriodCountDisplay));
        OnPropertyChanged(nameof(EditorModeTitle));
        OnPropertyChanged(nameof(EditorHelpText));
        OnPropertyChanged(nameof(HasSelectedPeriod));
        OnPropertyChanged(nameof(CanToggleSelectedPeriodLock));
        OnPropertyChanged(nameof(SelectedPeriodIsLocked));
        OnPropertyChanged(nameof(SelectedPeriodNameDisplay));
        OnPropertyChanged(nameof(SelectedPeriodCoverageDisplay));
        OnPropertyChanged(nameof(SelectedPeriodDateRangeDisplay));
        OnPropertyChanged(nameof(SelectedPeriodDurationDisplay));
        OnPropertyChanged(nameof(SelectedPeriodStatusDisplay));
        RefreshPeriodCommandState();
    }

    private void RefreshPeriodCommandState()
    {
        CloseSelectedPeriodCommand.NotifyCanExecuteChanged();
        ReopenSelectedPeriodCommand.NotifyCanExecuteChanged();
        DeleteSelectedPeriodCommand.NotifyCanExecuteChanged();
    }

    private sealed record FiscalYearDraftSnapshot(string Name, DateTime StartDate, DateTime EndDate, bool IsLocked);

    private sealed record FiscalYearRange(DateTime StartDate, DateTime EndDate);
}