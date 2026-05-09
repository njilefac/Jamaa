using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel.DataAnnotations;
using Domain.Finances.Values;
using Jamaa.Application.Finances;
using Jamaa.Application.Shared;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Services.Notifications;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class ChartOfAccountsViewModel : ValidatableFormViewModel, IApplicationModule, IRouteableViewModel
{
    private readonly IFinanceManagementFacade _financeFacade;
    private readonly IUserSessionService _userSessionService;
    private readonly INotificationService _notificationService;
    private readonly List<AccountData> _allAccountData = [];

    public Guid Id => Guid.Parse("e2d9f6b1-8e4a-4d9c-8f3b-2a3c4d5e6f7a");
    public string Title => "Chart of Accounts";
    public object? HeaderContent => null;

    [ObservableProperty]
    private string _pageTitle = "Chart of Accounts";

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accounts = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteAccountCommand))]
    [NotifyPropertyChangedFor(nameof(DeleteAccountTooltip))]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddAccountCommand))]
    private AccountType? _selectedAccountType;

    [ObservableProperty]
    private AccountItemViewModel? _selectedParentAccount;

    public ObservableCollection<AccountItemViewModel> FilteredParentAccounts { get; } = [];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Account code is required")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Code must be numeric")]
    [CustomValidation(typeof(ChartOfAccountsViewModel), nameof(ValidateAccountCode))]
    [NotifyCanExecuteChangedFor(nameof(AddAccountCommand))]
    private string _accountCode = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Account name is required")]
    [NotifyPropertyChangedFor(nameof(AccountCode))]
    [NotifyCanExecuteChangedFor(nameof(AddAccountCommand))]
    private string _accountName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Description must be 500 characters or fewer")]
    [NotifyCanExecuteChangedFor(nameof(AddAccountCommand))]
    private string _accountDescription = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddAccountCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAccountCommand))]
    private bool _isOperationInFlight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    public string ActionButtonText => SelectedAccount == null ? "Add Account" : "Save Changes";
    public string FormTitle => SelectedAccount == null ? "Add New Account" : "Edit Account";
    public bool IsEditMode => SelectedAccount != null;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public string DeleteAccountTooltip => SelectedAccountHasChildren
        ? "This account cannot be deleted because it has child accounts."
        : "Permanently delete this account";

    public AccountType[] AccountTypes { get; } = Enum.GetValues<AccountType>();

    [ObservableProperty]
    private HierarchicalTreeDataGridSource<AccountItemViewModel>? _source;

    public ChartOfAccountsViewModel(
        IFinanceManagementFacade financeFacade,
        IUserSessionService userSessionService,
        IQueryProcessor queryProcessor,
        INotificationService notificationService)
    {
        _financeFacade = financeFacade;
        _userSessionService = userSessionService;
        _ = queryProcessor;
        _notificationService = notificationService;

        InitializeSource();
        LoadAccounts();
        if (SynchronizationContext.Current is { } syncContext)
        {
            SetupReactiveUpdates(syncContext);
        }
        else
        {
            SetupReactiveUpdates(null);
        }

        // Re-evaluate AddAccountCommand whenever validation errors change
        ErrorsChanged += (_, _) => AddAccountCommand.NotifyCanExecuteChanged();
    }

    // Operation: marshals observable notifications onto the UI synchronization context only when one exists.
    private static IObservable<T> ObserveOnIfAvailable<T>(IObservable<T> source, SynchronizationContext? syncContext)
    {
        return syncContext is null ? source : source.ObserveOn(syncContext);
    }

    private void InitializeSource()
    {
        Source = new HierarchicalTreeDataGridSource<AccountItemViewModel>(Accounts)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<AccountItemViewModel>(
                    new TextColumn<AccountItemViewModel, string>("Code", x => x.Code, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                    x => x.SubAccounts),
                new TextColumn<AccountItemViewModel, string>("Name", x => x.Name, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TextColumn<AccountItemViewModel, string>("Description", x => x.Description, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TextColumn<AccountItemViewModel, string>("Type", x => x.TypeDisplay, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
            }
        };

        var selection = new TreeDataGridRowSelectionModel<AccountItemViewModel>(Source)
        {
            SingleSelect = true
        };

        Source.Selection = selection;

        selection.SelectionChanged += (_, _) =>
        {
            SelectedAccount = selection.SelectedItem;
        };
    }


    private void SetupReactiveUpdates(SynchronizationContext? syncContext)
    {
        ObserveOnIfAvailable(
                _financeFacade.AccountCreated
                    .Merge(_financeFacade.AccountUpdated)
                    .Merge(_financeFacade.AccountDeleted)
                    .Merge(_financeFacade.AccountDeactivated)
                    .Merge(_financeFacade.AccountReactivated)
                    .Throttle(TimeSpan.FromMilliseconds(100)),
                syncContext)
            .Subscribe(_ => LoadAccounts());
    }

    private async void LoadAccounts()
    {
        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var accounts = await _financeFacade.GetAccounts(session.Organisation.Id);
        _allAccountData.Clear();
        _allAccountData.AddRange(accounts);

        Accounts.Clear();
        var rootAccounts = BuildAccountTree(accounts);
        foreach (var root in rootAccounts)
        {
            Accounts.Add(root);
        }

        RefreshFilteredParentAccounts();
    }

    private List<AccountItemViewModel> BuildAccountTree(IEnumerable<AccountData> accounts)
    {
        var accountList = accounts.ToList();

        var viewModels = accountList.Select(a =>
        {
            var vm = new AccountItemViewModel
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                Type = a.Type,
                IsActive = a.IsActive
            };
            AssignItemCommands(vm);
            return vm;
        }).ToList();

        var lookup = viewModels.ToDictionary(a => a.Id);
        var roots = new List<AccountItemViewModel>();

        foreach (var accountData in accountList)
        {
            var vm = lookup[accountData.Id];
            if (accountData.ParentId != null && lookup.TryGetValue(accountData.ParentId, out var parentVm))
            {
                vm.Parent = parentVm;
                parentVm.SubAccounts.Add(vm);
            }
            else
            {
                roots.Add(vm);
            }
        }

        return roots;
    }

    partial void OnSelectedAccountChanged(AccountItemViewModel? value)
    {
        if (value != null)
        {
            AccountCode = value.Code;
            AccountName = value.Name;
            AccountDescription = value.Description;
            SelectedAccountType = value.Type;
            
            RefreshFilteredParentAccounts();

            // Map parent to the reference in our collection for Avalonia selection to work
            if (value.Parent != null)
            {
                SelectedParentAccount = FilteredParentAccounts
                    .FirstOrDefault(a => a.Id == value.Parent.Id);
            }
            else
            {
                SelectedParentAccount = null;
            }
        }
        else
        {
            AccountCode = string.Empty;
            AccountName = string.Empty;
            AccountDescription = string.Empty;
            SelectedAccountType = null;
            RefreshFilteredParentAccounts();
            SelectedParentAccount = null;
        }

        OnPropertyChanged(nameof(ActionButtonText));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    public static ValidationResult? ValidateAccountCode(string code, ValidationContext context)
    {
        var vm = (ChartOfAccountsViewModel)context.ObjectInstance;
        if (vm.SelectedAccountType == null)
            return ValidationResult.Success;

        if (!vm.IsCodeInRange(vm.SelectedAccountType.Value, code, vm.AccountName))
        {
            var (min, max) = vm.GetCodeRange(vm.SelectedAccountType.Value, vm.AccountName);
            return new ValidationResult($"Code for {vm.SelectedAccountType.Value} must be between {min} and {max}");
        }

        return ValidationResult.Success;
    }

    partial void OnSelectedAccountTypeChanged(AccountType? value)
    {
        _ = value;
        RefreshFilteredParentAccounts();
        SuggestAccountCode();
        ValidateProperty(AccountCode, nameof(AccountCode));
    }

    private void SuggestAccountCode()
    {
        if (IsEditMode || SelectedAccountType == null) return;

        var (min, max) = GetCodeRange(SelectedAccountType.Value, AccountName);
        
        var existingCodes = _allAccountData
            .Where(a => int.TryParse(a.Code, out var c) && c >= min && c <= max)
            .Select(a => int.Parse(a.Code))
            .ToList();

        if (existingCodes.Count == 0)
        {
            AccountCode = min.ToString();
        }
        else
        {
            var nextCode = existingCodes.Max() + 1;
            if (nextCode <= max)
            {
                AccountCode = nextCode.ToString();
            }
        }
    }

    partial void OnAccountNameChanged(string value)
    {
        _ = value;
        if (SelectedAccountType == AccountType.Expense)
        {
            SuggestAccountCode();
            ValidateProperty(AccountCode, nameof(AccountCode));
        }
    }

    // OPERATION: returns a code range per account type and account naming policy.
    private (int Min, int Max) GetCodeRange(AccountType type, string accountName)
    {
        // Expense fees are grouped in a separate 6000-range bucket.
        if (type == AccountType.Expense && accountName.Contains("fee", StringComparison.OrdinalIgnoreCase))
        {
            return (6000, 6999);
        }

        return type switch
        {
            AccountType.Asset => (1000, 1999),
            AccountType.Liability => (2000, 2999),
            AccountType.Equity => (3000, 3999),
            AccountType.Revenue => (4000, 4999),
            AccountType.Expense => (5000, 5999),
            _ => (1000, 9999)
        };
    }

    // OPERATION: validates code parsing and range constraints.
    private bool IsCodeInRange(AccountType type, string code, string accountName)
    {
        if (!int.TryParse(code, out var numericCode))
        {
            return false;
        }

        var (min, max) = GetCodeRange(type, accountName);
        return numericCode >= min && numericCode <= max;
    }

    private void RefreshFilteredParentAccounts()
    {
        var previousSelection = SelectedParentAccount;
        FilteredParentAccounts.Clear();
        var allAccounts = GetAllAccounts(Accounts);
        foreach (var account in allAccounts)
        {
            if (CanBeParent(account))
            {
                FilteredParentAccounts.Add(account);
            }
        }
        
        // Try to restore selection if it's still valid in the new filtered list
        if (previousSelection != null)
        {
            SelectedParentAccount = FilteredParentAccounts.FirstOrDefault(a => a.Id == previousSelection.Id);
        }
    }

    private IEnumerable<AccountItemViewModel> GetAllAccounts(IEnumerable<AccountItemViewModel> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in GetAllAccounts(root.SubAccounts))
            {
                yield return child;
            }
        }
    }

    private bool CanBeParent(AccountItemViewModel potentialParent)
    {
        // 1. Must be of the selected type
        if (SelectedAccountType.HasValue && potentialParent.Type != SelectedAccountType.Value)
            return false;

        // 2. Cannot be itself
        if (SelectedAccount != null && potentialParent.Id == SelectedAccount.Id)
            return false;

        // 3. No cycles
        if (SelectedAccount != null && IsDescendantOf(potentialParent, SelectedAccount))
            return false;

        return true;
    }

    private bool IsDescendantOf(AccountItemViewModel node, AccountItemViewModel potentialAncestor)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current.Id == potentialAncestor.Id)
                return true;
            current = current.Parent;
        }
        return false;
    }

    // Operation: wires the row-level action commands to the parent ViewModel operations for one item.
    private void AssignItemCommands(AccountItemViewModel item)
    {
        item.EditCommand = new RelayCommand(() => SelectAccountForEdit(item));
        item.ToggleActiveCommand = new AsyncRelayCommand(() => ToggleAccountActiveAsync(item));
        item.ViewLedgerCommand = new RelayCommand(() => NavigateToAccountLedger(item));
    }

    // Operation: selects the given account for editing in the side form.
    private void SelectAccountForEdit(AccountItemViewModel item)
    {
        SelectedAccount = item;
    }

    // Integration: toggles one account between active and inactive states.
    private async Task ToggleAccountActiveAsync(AccountItemViewModel item)
    {
        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var orgId = session.Organisation.Id;
        var accountId = item.Id;
        var subject = item.Name;

        if (item.IsActive)
        {
            await _notificationService.TrackOperationAsync(
                sendCommand: () => _financeFacade.DeactivateAccount(orgId, accountId),
                confirmationObservable: BuildAccountStateChangeConfirmationObservable(
                    orgId,
                    accountId,
                    isActiveTarget: false,
                    eventStream: _financeFacade.AccountDeactivated),
                timeout: TimeSpan.FromSeconds(10),
                operationName: "Account",
                successAction: "Deactivated",
                subject: subject,
                inFlightChanged: SetOperationInFlight);
        }
        else
        {
            await _notificationService.TrackOperationAsync(
                sendCommand: () => _financeFacade.ReactivateAccount(orgId, accountId),
                confirmationObservable: BuildAccountStateChangeConfirmationObservable(
                    orgId,
                    accountId,
                    isActiveTarget: true,
                    eventStream: _financeFacade.AccountReactivated),
                timeout: TimeSpan.FromSeconds(10),
                operationName: "Account",
                successAction: "Reactivated",
                subject: subject,
                inFlightChanged: SetOperationInFlight);
        }
    }

    // Operation: confirms account state changes from either event notifications or eventual read-model state.
    private IObservable<bool> BuildAccountStateChangeConfirmationObservable(
        string organisationId,
        string accountId,
        bool isActiveTarget,
        IObservable<AccountData> eventStream)
    {
        var matchingEvents = eventStream
            .Where(account =>
                account.OrganisationId == organisationId &&
                account.Id == accountId &&
                account.IsActive == isActiveTarget)
            .Select(_ => true);

        var readModelStateChecks = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .StartWith(0L)
            .SelectMany(_ => Observable.FromAsync(() => HasAccountReachedStateAsync(organisationId, accountId, isActiveTarget)))
            .Where(hasReachedTargetState => hasReachedTargetState)
            .Select(_ => true);

        return matchingEvents
            .Merge(readModelStateChecks)
            .Take(1);
    }

    // Operation: checks whether one account currently matches the requested active state in the read model.
    private async Task<bool> HasAccountReachedStateAsync(string organisationId, string accountId, bool isActiveTarget)
    {
        try
        {
            var accounts = await _financeFacade.GetAccounts(organisationId);
            var account = accounts.FirstOrDefault(current => current.Id == accountId);
            return account is not null && account.IsActive == isActiveTarget;
        }
        catch
        {
            return false;
        }
    }

    // Operation: navigates to journal entries with account context for the given account.
    private void NavigateToAccountLedger(AccountItemViewModel item)
    {
        var navigationRequest = JournalEntriesNavigationRequest.ForAccount(item.Id, item.Code, item.Name);
        WeakReferenceMessenger.Default.Send(new ModuleSelected(Routes.AccountingTransactions, navigationRequest));
    }

    private bool CanAddAccount() =>
        !IsOperationInFlight &&
        !string.IsNullOrWhiteSpace(AccountCode) &&
        !string.IsNullOrWhiteSpace(AccountName) &&
        SelectedAccountType.HasValue &&
        !GetErrors(nameof(AccountCode)).Cast<object>().Any() &&
        !GetErrors(nameof(AccountName)).Cast<object>().Any() &&
        !GetErrors(nameof(AccountDescription)).Cast<object>().Any();

    private bool CanDeleteAccount() =>
        !IsOperationInFlight &&
        SelectedAccount != null &&
        !SelectedAccountHasChildren;

    // Operation: determines whether the selected account has child accounts and cannot be deleted.
    private bool SelectedAccountHasChildren => SelectedAccount?.SubAccounts.Count > 0;

    private void SetOperationInFlight(bool isInFlight)
    {
        IsOperationInFlight = isInFlight;
        AddAccountCommand.NotifyCanExecuteChanged();
        DeleteAccountCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddAccount))]
    private async Task AddAccount()
    {
        // INTEGRATION: Validates, sends command, waits for event confirmation
        if (!ValidateAndShow()) return;

        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var orgId = session.Organisation.Id;

        if (SelectedAccount != null)
        {
            var accountId = SelectedAccount.Id;
            var subject = AccountName;
            var isConfirmed = await _notificationService.TrackOperationAsync(
                sendCommand: () => _financeFacade.UpdateAccount(orgId, accountId, AccountCode, AccountName, AccountDescription, SelectedAccountType ?? AccountType.Asset, SelectedParentAccount?.Id),
                confirmationObservable: _financeFacade.AccountUpdated,
                matcherPredicate: a => a.Id == accountId,
                timeout: TimeSpan.FromSeconds(10),
                operationName: "Account",
                successAction: "Saved",
                subject: subject,
                inFlightChanged: SetOperationInFlight);

            if (isConfirmed)
                ResetForm();
        }
        else
        {
            var code = AccountCode.Trim();
            var name = AccountName.Trim();
            var subject = AccountName;
            var parentAccountId = SelectedParentAccount?.Id;
            var accountType = SelectedAccountType ?? AccountType.Asset;
            var confirmationSource = AccountCreationConfirmationSource.None;
            var isConfirmed = await _notificationService.TrackOperationAsync(
                sendCommand: () => _financeFacade.CreateAccount(orgId, code, AccountName, AccountDescription, accountType, parentAccountId),
                confirmationObservable: BuildAccountCreationConfirmationObservable(orgId, code, name, accountType, parentAccountId)
                    .Do(source => confirmationSource = source)
                    .Select(_ => true),
                timeout: TimeSpan.FromSeconds(10),
                operationName: "Account",
                successAction: "Created",
                subject: subject,
                inFlightChanged: SetOperationInFlight);

            UpdateCreateConfirmationStatus(confirmationSource, subject);

            if (isConfirmed)
                ResetForm();
        }
    }

    // Operation: confirms account creation from either change notifications or eventual read-model visibility.
    private IObservable<AccountCreationConfirmationSource> BuildAccountCreationConfirmationObservable(
        string organisationId,
        string code,
        string name,
        AccountType type,
        string? parentAccountId)
    {
        var createdEvents = _financeFacade.AccountCreated
            .Where(account =>
                account.OrganisationId == organisationId &&
                string.Equals(account.Code, code, StringComparison.Ordinal) &&
                string.Equals(account.Name, name, StringComparison.Ordinal) &&
                account.Type == type &&
                string.Equals(account.ParentId, parentAccountId, StringComparison.Ordinal))
            .Select(_ => AccountCreationConfirmationSource.EventStream);

        var readModelPresenceChecks = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .StartWith(0L)
            .SelectMany(_ => Observable.FromAsync(() => HasCreatedAccountAppearedAsync(organisationId, code, name, type, parentAccountId)))
            .Where(isPresent => isPresent)
            .Select(_ => AccountCreationConfirmationSource.ReadModel);

        return createdEvents
            .Merge(readModelPresenceChecks)
            .Take(1);
    }

    // Operation: publishes a small troubleshooting status message after account creation confirmation.
    private void UpdateCreateConfirmationStatus(AccountCreationConfirmationSource source, string subject)
    {
        var accountLabel = string.IsNullOrWhiteSpace(subject) ? "account" : subject;
        StatusMessage = source switch
        {
            AccountCreationConfirmationSource.EventStream => $"Created {accountLabel}: confirmed from event stream.",
            AccountCreationConfirmationSource.ReadModel => $"Created {accountLabel}: confirmed from read model.",
            _ => string.Empty
        };
    }

    // Operation: checks if the new account is already queryable from the materialized read model.
    private async Task<bool> HasCreatedAccountAppearedAsync(
        string organisationId,
        string code,
        string name,
        AccountType type,
        string? parentAccountId)
    {
        try
        {
            var accounts = await _financeFacade.GetAccounts(organisationId);
            return accounts.Any(account =>
                account.OrganisationId == organisationId &&
                string.Equals(account.Code, code, StringComparison.Ordinal) &&
                string.Equals(account.Name, name, StringComparison.Ordinal) &&
                account.Type == type &&
                string.Equals(account.ParentId, parentAccountId, StringComparison.Ordinal));
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAccount))]
    private async Task DeleteAccount()
    {
        // INTEGRATION: Sends delete command, waits for event confirmation
        if (SelectedAccount == null || SelectedAccountHasChildren) return;

        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var orgId = session.Organisation.Id;
        var accountId = SelectedAccount.Id;
        var subject = SelectedAccount.Name;

        var isConfirmed = await _notificationService.TrackOperationAsync(
            sendCommand: () => _financeFacade.DeleteAccount(orgId, accountId),
            confirmationObservable: _financeFacade.AccountDeleted,
            matcherPredicate: account => account.Id == accountId,
            timeout: TimeSpan.FromSeconds(10),
            operationName: "Account",
            successAction: "Deleted",
            subject: subject,
            inFlightChanged: SetOperationInFlight);

        if (isConfirmed)
        {
            LoadAccounts();
            ResetForm();
        }
    }

    [RelayCommand]
    public void ResetForm()
    {
        if (IsOperationInFlight)
        {
            return;
        }

        SelectedAccount = null;
        if (Source?.Selection is ITreeDataGridRowSelectionModel<AccountItemViewModel> selection)
        {
            selection.Clear();
        }
        AccountCode = string.Empty;
        AccountName = string.Empty;
        AccountDescription = string.Empty;
        SelectedAccountType = null;
        SelectedParentAccount = null;
        ResetValidationState();
    }

    private enum AccountCreationConfirmationSource
    {
        None,
        EventStream,
        ReadModel
    }
}

