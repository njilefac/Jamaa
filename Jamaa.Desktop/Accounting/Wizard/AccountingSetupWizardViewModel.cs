using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;
using Jamaa.Desktop.Shared.Controls;

namespace Jamaa.Desktop.Accounting.Wizard;

public partial class AccountingSetupWizardViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRouteResolver _routeResolver;

    [ObservableProperty]
    private int _currentStepIndex;

    [ObservableProperty]
    private object? _currentStepContent;

    public AccountingSetupWizardViewModel(
        IServiceProvider serviceProvider,
        IRouteResolver routeResolver)
    {
        _serviceProvider = serviceProvider;
        _routeResolver = routeResolver;
        InitializeSteps();
        UpdateCurrentStep();
    }

    public ObservableCollection<WizardStepViewModel> Steps { get; } = [];

    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
    public string Title => "Accounting Setup Wizard";
    public object? HeaderContent => null;

    private void InitializeSteps()
    {
        Steps.Add(new WizardStepViewModel("Fiscal Calendar & Currency", "Phase 1: Define your fiscal year and operational currency.", stepNumber: 1));
        Steps.Add(new WizardStepViewModel("Chart of Accounts", "Phase 2: Build your account structure and set opening balances for leaf accounts.", stepNumber: 2));
        Steps.Add(new WizardStepViewModel("Final Review", "Phase 3: Review and initialize the ledger.", stepNumber: 3, hasConnector: false));
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStepIndex < Steps.Count - 1)
        {
            Steps[CurrentStepIndex].IsCompleted = true;
            CurrentStepIndex++;
            UpdateCurrentStep();
        }
    }

    private bool CanGoNext() => CurrentStepIndex < Steps.Count - 1;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            UpdateCurrentStep();
        }
    }

    private bool CanGoBack() => CurrentStepIndex > 0;

    private void UpdateCurrentStep()
    {
        // Integration: Resolve the appropriate view model for the current step
        CurrentStepContent = CurrentStepIndex switch
        {
            0 => _serviceProvider.GetService(typeof(FiscalCalendarAndCurrencyStepViewModel)),
            1 => _routeResolver.Resolve(Routes.ChartOfAccounts),
            2 => _serviceProvider.GetService(typeof(FinalReviewStepViewModel)),
            _ => null
        };

        // Update IsEnabled for future steps (simple logic: current and previous steps are enabled)
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsEnabled = i <= CurrentStepIndex || Steps[i].IsCompleted;
        }
        
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }
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
