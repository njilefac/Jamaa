using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Jamaa.Application.Finances;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class FiscalCalendarAndPeriodsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel, IDisposable
{
    private static readonly ReadOnlyObservableCollection<FiscalYearEditorItemViewModel> EmptyFiscalYears =
        new(new ObservableCollection<FiscalYearEditorItemViewModel>());
    private static readonly ReadOnlyObservableCollection<AccountingPeriodItemViewModel> EmptyPeriods =
        new(new ObservableCollection<AccountingPeriodItemViewModel>());
    private readonly SourceList<FiscalYearEditorItemViewModel> _fiscalYearsSource = new();
    private readonly IDisposable _fiscalYearsSubscription;
    private readonly IDisposable _fiscalYearsStreamSubscription;
    private FiscalYearDraftSnapshot? _selectedFiscalYearSnapshot;
    private readonly IFinanceManagementFacade _financeManagementFacade;
    private readonly string _organisationId;

    public FiscalCalendarAndPeriodsViewModel(
        IFinanceManagementFacade financeManagementFacade,
        IUserSessionService userSessionService)
    {
        var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _financeManagementFacade = financeManagementFacade;
        _organisationId = ResolveOrganisationId(userSessionService);

        _fiscalYearsSubscription = _fiscalYearsSource.Connect()
            .AutoRefresh(static fiscalYear => fiscalYear.StartDate)
            .Sort(SortExpressionComparer<FiscalYearEditorItemViewModel>.Descending(static fiscalYear => fiscalYear.StartDate))
            .Bind(out ReadOnlyObservableCollection<FiscalYearEditorItemViewModel> fiscalYearsBound)
            .Subscribe();

        FiscalYears = fiscalYearsBound;

        _fiscalYearsStreamSubscription = _financeManagementFacade
            .GetFiscalYearsStream(_organisationId)
            .ObserveOn(syncContext)
            .Subscribe(ApplyFiscalYears, HandleFiscalYearsStreamError);
    }

    // Operation: resolves the current organisation id from the active user session
    private static string ResolveOrganisationId(IUserSessionService userSessionService)
    {
        return userSessionService.CurrentUserSession?.Organisation?.Id
               ?? throw new InvalidOperationException("An active session with an organisation is required to manage fiscal calendars.");
    }

    public Guid Id => Guid.Parse("76a6a087-42cf-4495-8d6f-48ec84f917da");
    public string Title => "Fiscal Calendar & Periods";
    public object? HeaderContent => null;

    public bool HasSelectedFiscalYear => SelectedFiscalYear is not null;

    public bool HasSelectedPeriod => SelectedPeriod is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string SelectedFiscalYearNameDisplay => SelectedFiscalYear?.Name ?? "No fiscal year selected";

    public string SelectedFiscalYearTimelineDisplay => SelectedFiscalYear?.DateRangeLabel ?? "Choose a fiscal year to inspect and edit its generated accounting periods.";

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

    public string EditorModeTitle => SelectedFiscalYear is null ? "Select a fiscal year" : $"Editing {SelectedFiscalYear.Name}";

    public string EditorHelpText => SelectedFiscalYear is null
        ? "Pick a fiscal year from the left pane to review the year details and its generated accounting periods."
        : "Adjust the date range or locked status, then save. Accounting periods remain aligned to the selected fiscal year.";

    public string SelectedPeriodNameDisplay => SelectedPeriod?.Name ?? "Select an accounting period";

    public string SelectedPeriodCoverageDisplay => SelectedPeriod?.CoverageLabel ?? "Choose one of the generated periods to inspect its coverage.";

    public string SelectedPeriodDateRangeDisplay => SelectedPeriod?.DateRangeLabel ?? "—";

    public string SelectedPeriodDurationDisplay => SelectedPeriod?.DurationLabel ?? "—";

    public string SelectedPeriodStatusDisplay => SelectedPeriod?.StatusLabel ?? "—";

    public bool CanToggleFiscalYearLock => SelectedFiscalYear is { IsLocked: false };

    public bool CanToggleSelectedPeriodLock => SelectedPeriod is { IsLocked: false };

    public bool SelectedPeriodIsLocked
    {
        get => SelectedPeriod?.IsLocked ?? false;
        set
        {
            // Unlocking is intentionally blocked for now; future authorization can enable it.
            if (!value || SelectedPeriod is null || SelectedPeriod.IsLocked)
            {
                return;
            }

            if (CloseSelectedPeriodCommand.CanExecute(null))
            {
                CloseSelectedPeriodCommand.Execute(null);
            }
        }
    }

    [ObservableProperty]
    private ReadOnlyObservableCollection<FiscalYearEditorItemViewModel> _fiscalYears = EmptyFiscalYears;

    [ObservableProperty]
    private ReadOnlyObservableCollection<AccountingPeriodItemViewModel> _visiblePeriods = EmptyPeriods;

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
    private FiscalYearEditorItemViewModel? _selectedFiscalYear;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPeriod))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodNameDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodCoverageDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodDateRangeDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodDurationDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedPeriodStatusDisplay))]
    [NotifyCanExecuteChangedFor(nameof(CloseSelectedPeriodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReopenSelectedPeriodCommand))]
    private AccountingPeriodItemViewModel? _selectedPeriod;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private DateTime _draftStartDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private DateTime _draftEndDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFiscalYearCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertFiscalYearCommand))]
    private bool _draftIsLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    partial void OnSelectedFiscalYearChanged(FiscalYearEditorItemViewModel? value)
    {
        LoadDraftFromSelection(value);
        VisiblePeriods = value?.Periods ?? EmptyPeriods;
        SelectedPeriod = VisiblePeriods.FirstOrDefault();
        OnPropertyChanged(nameof(CanToggleFiscalYearLock));
    }

    partial void OnSelectedPeriodChanged(AccountingPeriodItemViewModel? value)
    {
        _ = value;
        OnPropertyChanged(nameof(CanToggleSelectedPeriodLock));
        OnPropertyChanged(nameof(SelectedPeriodIsLocked));
        RefreshPeriodCommandState();
    }

    [RelayCommand]
    private async Task AddFiscalYear()
    {
        var (startDate, endDate) = GetNextAvailableFiscalYearRange();

        await _financeManagementFacade.CreateFiscalYear(_organisationId, startDate, endDate, false);
        StatusMessage = $"Created fiscal year {startDate:yyyy}-{endDate:yy}";
        // UI will update automatically via the reactive stream subscription
    }

    [RelayCommand(CanExecute = nameof(CanSaveFiscalYear))]
    private async Task SaveFiscalYear()
    {
        if (SelectedFiscalYear is null)
        {
            return;
        }

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

        await _financeManagementFacade.UpdateFiscalYear(_organisationId, fiscalYear.Id.ToString(), draftStart, draftEnd, draftLocked);
        ApplyDraftToSelectedFiscalYear(fiscalYear, draftStart, draftEnd, draftLocked);
        _selectedFiscalYearSnapshot = new FiscalYearDraftSnapshot(fiscalYear.Name, draftStart, draftEnd, draftLocked);
        SelectedFiscalYear = fiscalYear;
        SelectedPeriod = fiscalYear.Periods.FirstOrDefault();
        StatusMessage = $"Saved {fiscalYear.Name}.";
        RaiseSelectionStateChanged();
        // UI will update automatically via the reactive stream subscription
    }

    [RelayCommand(CanExecute = nameof(CanDeleteFiscalYear))]
    private async Task DeleteFiscalYear()
    {
        if (SelectedFiscalYear is null)
        {
            return;
        }

        if (FiscalYears.Count == 1)
        {
            StatusMessage = "Keep at least one fiscal year in the timeline.";
            return;
        }

        var deletedFiscalYear = SelectedFiscalYear;
        await _financeManagementFacade.DeleteFiscalYear(_organisationId, deletedFiscalYear.Id.ToString());
        SelectedFiscalYear = FiscalYears.FirstOrDefault();
        StatusMessage = $"Deleted {deletedFiscalYear.Name}.";
        // UI will update automatically via the reactive stream subscription
    }

    [RelayCommand(CanExecute = nameof(CanRevertFiscalYear))]
    private void RevertFiscalYear()
    {
        if (_selectedFiscalYearSnapshot is null)
        {
            return;
        }

        DraftStartDate = _selectedFiscalYearSnapshot.StartDate;
        DraftEndDate = _selectedFiscalYearSnapshot.EndDate;
        DraftIsLocked = _selectedFiscalYearSnapshot.IsLocked;
        StatusMessage = $"Reverted the editor back to {_selectedFiscalYearSnapshot.Name}.";
    }

    [RelayCommand(CanExecute = nameof(CanRegeneratePeriods))]
    private Task RegeneratePeriods()
    {
        if (SelectedFiscalYear is null)
        {
            return Task.CompletedTask;
        }

        SelectedFiscalYear.ReplacePeriods(GenerateAccountingPeriods(SelectedFiscalYear.StartDate, SelectedFiscalYear.EndDate, SelectedFiscalYear.IsLocked));
        SynchronizeFiscalYearAndPeriodLockState(SelectedFiscalYear);
        SelectedPeriod = SelectedFiscalYear.Periods.FirstOrDefault();
        StatusMessage = $"Regenerated {SelectedFiscalYear.Periods.Count} accounting periods for {SelectedFiscalYear.Name}.";
        RaiseSelectionStateChanged();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanCloseSelectedPeriod))]
    private async Task CloseSelectedPeriod()
    {
        if (SelectedFiscalYear is null || SelectedPeriod is null)
        {
            return;
        }

        await _financeManagementFacade.UpdateAccountingPeriod(
            _organisationId,
            SelectedFiscalYear.Id.ToString(),
            SelectedPeriod.Id,
            SelectedPeriod.SequenceNumber,
            SelectedPeriod.StartDate,
            SelectedPeriod.EndDate,
            true);

        SelectedPeriod.IsLocked = true;
        SynchronizeFiscalYearAndPeriodLockState(SelectedFiscalYear);
        DraftIsLocked = SelectedFiscalYear.IsLocked;
        StatusMessage = $"Closed {SelectedPeriod.Name}. The fiscal year closes automatically once all periods are closed.";
        RaiseSelectionStateChanged();
        RefreshPeriodCommandState();
        // UI will update automatically via the reactive stream subscription
    }

    [RelayCommand(CanExecute = nameof(CanReopenSelectedPeriod))]
    private async Task ReopenSelectedPeriod()
    {
        if (SelectedFiscalYear is null || SelectedPeriod is null)
        {
            return;
        }

        await _financeManagementFacade.UpdateAccountingPeriod(
            _organisationId,
            SelectedFiscalYear.Id.ToString(),
            SelectedPeriod.Id,
            SelectedPeriod.SequenceNumber,
            SelectedPeriod.StartDate,
            SelectedPeriod.EndDate,
            false);

        SelectedPeriod.IsLocked = false;
        SynchronizeFiscalYearAndPeriodLockState(SelectedFiscalYear);
        DraftIsLocked = SelectedFiscalYear.IsLocked;
        StatusMessage = $"Reopened {SelectedPeriod.Name}. A fiscal year cannot remain locked while one of its periods is open.";
        RaiseSelectionStateChanged();
        RefreshPeriodCommandState();
        // UI will update automatically via the reactive stream subscription
    }

    private bool CanSaveFiscalYear()
    {
        return SelectedFiscalYear is not null && DraftEndDate.Date >= DraftStartDate.Date;
    }

    private bool CanDeleteFiscalYear()
    {
        return SelectedFiscalYear is not null;
    }

    private bool CanRevertFiscalYear()
    {
        return SelectedFiscalYear is not null && _selectedFiscalYearSnapshot is not null;
    }

    private bool CanRegeneratePeriods()
    {
        return SelectedFiscalYear is not null;
    }

    private bool CanCloseSelectedPeriod()
    {
        return SelectedPeriod is { IsLocked: false };
    }

    private bool CanReopenSelectedPeriod()
    {
        return SelectedPeriod is { IsLocked: true };
    }

    // Operation: projects streamed fiscal-year data into UI view models.
    private void ApplyFiscalYears(IList<FiscalYearData> fiscalYearData)
    {
        var selectedFiscalYearId = SelectedFiscalYear?.Id;

        _fiscalYearsSource.Edit(updater =>
        {
            updater.Clear();
            foreach (var fyData in fiscalYearData.OrderByDescending(fy => fy.StartDate))
            {
                var periods = fyData.Periods
                    .OrderBy(p => p.SequenceNumber)
                    .Select(p => new AccountingPeriodItemViewModel(p.Id, p.SequenceNumber, p.StartDate, p.EndDate, p.IsLocked))
                    .ToList();

                var viewModel = new FiscalYearEditorItemViewModel(Guid.Parse(fyData.Id), fyData.StartDate, fyData.EndDate, fyData.IsLocked);
                viewModel.ReplacePeriods(periods);
                updater.Add(viewModel);
            }
        });

        SelectedFiscalYear = selectedFiscalYearId is null
            ? FiscalYears.FirstOrDefault()
            : FiscalYears.FirstOrDefault(fiscalYear => fiscalYear.Id == selectedFiscalYearId) ?? FiscalYears.FirstOrDefault();

        RaiseCollectionSummaryChanged();
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
        _selectedFiscalYearSnapshot = new FiscalYearDraftSnapshot(fiscalYear.Name, fiscalYear.StartDate, fiscalYear.EndDate, fiscalYear.IsLocked);
    }

    // Operation: applies the current draft values to the selected fiscal year view model (optimistic update).
    private void ApplyDraftToSelectedFiscalYear(FiscalYearEditorItemViewModel fiscalYear, DateTime startDate, DateTime endDate, bool isLocked)
    {
        fiscalYear.StartDate = startDate.Date;
        fiscalYear.EndDate = endDate.Date;
        fiscalYear.IsLocked = isLocked;
        fiscalYear.RefreshName();
        fiscalYear.ReplacePeriods(GenerateAccountingPeriods(startDate, endDate, isLocked));
    }

    // Operation: computes the next contiguous one-year fiscal span with no gaps.
    private (DateTime StartDate, DateTime EndDate) GetNextAvailableFiscalYearRange()
    {
        var newestFiscalYear = FiscalYears.MaxBy(static fiscalYear => fiscalYear.EndDate);
        var startDate = newestFiscalYear?.EndDate.AddDays(1) ?? new DateTime(DateTime.Today.Year, 1, 1);
        var endDate = startDate.AddYears(1).AddDays(-1);
        return (startDate, endDate);
    }

    private static IReadOnlyList<AccountingPeriodItemViewModel> GenerateAccountingPeriods(DateTime startDate, DateTime endDate, bool isLocked)
    {
        var accountingPeriods = new List<AccountingPeriodItemViewModel>();
        var currentStartDate = startDate.Date;
        var sequenceNumber = 1;

        while (currentStartDate <= endDate.Date)
        {
            var currentEndDate = currentStartDate.AddMonths(1).AddDays(-1);

            if (currentEndDate > endDate.Date)
            {
                currentEndDate = endDate.Date;
            }

            // Generate temporary ID for UI preview; will be replaced when loaded from database
            var tempId = $"temp-period-{Guid.NewGuid():N}";
            accountingPeriods.Add(new AccountingPeriodItemViewModel(tempId, sequenceNumber, currentStartDate, currentEndDate, isLocked));
            currentStartDate = currentEndDate.AddDays(1);
            sequenceNumber++;
        }

        return accountingPeriods;
    }


    private static void SynchronizeFiscalYearAndPeriodLockState(FiscalYearEditorItemViewModel fiscalYear)
    {
        if (fiscalYear.IsLocked)
        {
            foreach (var period in fiscalYear.Periods)
            {
                period.IsLocked = true;
            }
        }

        if (AllPeriodsAreClosedAndCoverWholeYear(fiscalYear))
        {
            fiscalYear.IsLocked = true;
            return;
        }

        if (fiscalYear.Periods.Any(static period => !period.IsLocked))
        {
            fiscalYear.IsLocked = false;
        }
    }

    private static bool AllPeriodsAreClosedAndCoverWholeYear(FiscalYearEditorItemViewModel fiscalYear)
    {
        if (fiscalYear.Periods.Count == 0 || fiscalYear.Periods.Any(static period => !period.IsLocked))
        {
            return false;
        }

        var expectedStartDate = fiscalYear.StartDate.Date;

        foreach (var period in fiscalYear.Periods.OrderBy(static period => period.StartDate))
        {
            if (period.StartDate.Date != expectedStartDate || period.EndDate.Date < period.StartDate.Date)
            {
                return false;
            }

            expectedStartDate = period.EndDate.Date.AddDays(1);
        }

        return expectedStartDate == fiscalYear.EndDate.Date.AddDays(1);
    }

    public void Dispose()
    {
        _fiscalYearsStreamSubscription.Dispose();
        _fiscalYearsSubscription.Dispose();
        _fiscalYearsSource.Dispose();
    }

    private void RaiseCollectionSummaryChanged()
    {
        OnPropertyChanged(nameof(TotalFiscalYearsDisplay));
        OnPropertyChanged(nameof(LockedFiscalYearsDisplay));
        OnPropertyChanged(nameof(OpenFiscalYearsDisplay));
        OnPropertyChanged(nameof(SelectedFiscalYearPeriodCountDisplay));
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
    }

    private sealed record FiscalYearDraftSnapshot(string Name, DateTime StartDate, DateTime EndDate, bool IsLocked);
}