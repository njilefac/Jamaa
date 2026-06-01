using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;
using Jamaa.Desktop.Shared.Controls;

namespace Jamaa.Desktop.Accounting.Wizard;

public partial class AccountingSetupWizardViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    private readonly IAccountingFacade _accountingFacade;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRouteResolver _routeResolver;
    private readonly CompositeDisposable _subscriptions = [];
    private readonly string? _organisationId;
    private bool _hasLoadedStepState;

    [ObservableProperty]
    private int _currentStepIndex;

    [ObservableProperty]
    private object? _currentStepContent;

    public AccountingSetupWizardViewModel(
        IServiceProvider serviceProvider,
        IAccountingFacade accountingFacade,
        IUserSessionService userSessionService,
        IRouteResolver routeResolver)
    {
        _serviceProvider = serviceProvider;
        _accountingFacade = accountingFacade;
        _routeResolver = routeResolver;
        _organisationId = ResolveOrganisationId(userSessionService);
        InitializeSteps();
        SubscribeToAccountingChanges();
        _ = RefreshStepStateAsync();
    }

    public ObservableCollection<WizardStepViewModel> Steps { get; } = [];

    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
    public string Title => "Accounting Setup Wizard";
    public object? HeaderContent => null;

    private void InitializeSteps()
    {
        Steps.Add(new WizardStepViewModel("Fiscal Calendar & Currency", "Phase 1: Define your fiscal year and operational currency.", stepNumber: 1));
        Steps.Add(new WizardStepViewModel("Chart of Accounts", "Phase 2: Build your account structure.", stepNumber: 2));
        Steps.Add(new WizardStepViewModel("Final Review", "Phase 3: Review and initialize the ledger.", stepNumber: 3, hasConnector: false));
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (!CanGoNext())
        {
            return;
        }

        CurrentStepIndex++;
        UpdateCurrentStep();
    }

    private bool CanGoNext() => IsValidStepIndex(CurrentStepIndex) && Steps[CurrentStepIndex].IsCompleted && CurrentStepIndex < Steps.Count - 1;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (!CanGoBack())
        {
            return;
        }

        CurrentStepIndex--;
        UpdateCurrentStep();
    }

    private bool CanGoBack() => IsValidStepIndex(CurrentStepIndex) && CurrentStepIndex > 0;

    private void UpdateCurrentStep()
    {
        // Integration: Resolve the appropriate view model for the current step.
        CurrentStepContent = CurrentStepIndex switch
        {
            0 => _serviceProvider.GetService(typeof(FiscalCalendarAndCurrencyStepViewModel)),
            1 => _routeResolver.Resolve(Routes.ChartOfAccounts),
            2 => _serviceProvider.GetService(typeof(FinalReviewStepViewModel)),
            _ => null
        };

        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    private async Task RefreshStepStateAsync()
    {
        if (_organisationId is null)
        {
            ApplyStepState(new WizardCompletionState(false, false, false));
            return;
        }

        var settingsTask = _accountingFacade.GetAccountingSettings(_organisationId);
        var fiscalCalendarTask = _accountingFacade.GetFiscalCalendar(_organisationId);
        var chartOfAccountsTask = _accountingFacade.GetChartOfAccounts(_organisationId);
        var setupCompleteTask = _accountingFacade.IsAccountingSetupComplete(_organisationId);

        await Task.WhenAll(settingsTask, fiscalCalendarTask, chartOfAccountsTask, setupCompleteTask);

        var settings = await settingsTask;
        var fiscalCalendar = await fiscalCalendarTask;
        var chartOfAccounts = await chartOfAccountsTask;
        var setupComplete = await setupCompleteTask;

        ApplyStepState(new WizardCompletionState(
            StepOneIsComplete(settings, fiscalCalendar),
            chartOfAccounts.Accounts.Count > 0,
            setupComplete));
    }

    private void ApplyStepState(WizardCompletionState state)
    {
        Steps[0].IsCompleted = state.FiscalCalendarAndCurrencyCompleted;
        Steps[1].IsCompleted = state.ChartOfAccountsCompleted;
        Steps[2].IsCompleted = state.FinalReviewCompleted;

        Steps[0].IsEnabled = true;
        Steps[1].IsEnabled = Steps[0].IsCompleted;
        Steps[2].IsEnabled = Steps[1].IsCompleted;

        if (!_hasLoadedStepState || !IsValidStepIndex(CurrentStepIndex) || !Steps[CurrentStepIndex].IsEnabled)
        {
            CurrentStepIndex = GetPreferredStepIndex();
        }

        _hasLoadedStepState = true;
        UpdateCurrentStep();
    }

    private int GetPreferredStepIndex()
    {
        if (!Steps[0].IsCompleted) return 0;
        if (!Steps[1].IsCompleted) return 1;
        return 2;
    }

    private static bool StepOneIsComplete(
        AccountingSettingsData? settings,
        FiscalCalendarData fiscalCalendar)
    {
        return settings is { AvailableCurrencies.Count: > 0 } && fiscalCalendar.FiscalYears.Count > 0;
    }

    private void SubscribeToAccountingChanges()
    {
        if (_organisationId is null) return;

        var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _subscriptions.Add(
            Observable.Merge(
                    _accountingFacade.CurrentAccountingSettings
                        .Where(settings => settings?.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountingSettingsUpdated
                        .Where(settings => settings.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.CurrentFiscalCalendar
                        .Where(calendar => calendar.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.FiscalCalendarUpdated
                        .Where(calendar => calendar.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountCreated
                        .Where(account => account.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountUpdated
                        .Where(account => account.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountDeleted
                        .Where(account => account.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountDeactivated
                        .Where(account => account.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountReactivated
                        .Where(account => account.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default),
                    _accountingFacade.AccountOpeningBalanceSet
                        .Where(balance => balance.OrganisationId == _organisationId)
                        .Select(_ => Unit.Default))
                .ObserveOn(syncContext)
                .Subscribe(HandleAccountingChange));
    }

    private static string? ResolveOrganisationId(IUserSessionService userSessionService)
    {
        return userSessionService.CurrentUserSession?.Organisation?.Id;
    }

    private void HandleAccountingChange(Unit unit)
    {
        _ = RefreshStepStateAsync();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }

    private sealed record WizardCompletionState(
        bool FiscalCalendarAndCurrencyCompleted,
        bool ChartOfAccountsCompleted,
        bool FinalReviewCompleted);

    private bool IsValidStepIndex(int index) => index >= 0 && index < Steps.Count;
}

public partial class WizardStepViewModel : ObservableObject, IStepTimelineStep
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private int _stepNumber;
    [ObservableProperty] private bool _hasConnector;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isEnabled = true;

    public WizardStepViewModel(string title, string description, int stepNumber, bool hasConnector = true, bool isEnabled = true)
    {
        _title = title;
        _description = description;
        _stepNumber = stepNumber;
        _hasConnector = hasConnector;
        _isEnabled = isEnabled;
    }
}
