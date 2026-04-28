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
using Domain.Finances.Values;
using Jamaa.Application.Finances;
using Jamaa.Application.Shared;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class ChartOfAccountsViewModel : ValidatableFormViewModel, IApplicationModule, IRouteableViewModel
{
    private readonly IFinanceManagementFacade _financeFacade;
    private readonly IUserSessionService _userSessionService;
    private readonly IQueryProcessor _queryProcessor;
    private readonly List<AccountData> _allAccountData = [];

    public Guid Id => Guid.Parse("e2d9f6b1-8e4a-4d9c-8f3b-2a3c4d5e6f7a");
    public string Title => "Chart of Accounts";
    public object? HeaderContent => null;

    [ObservableProperty]
    private string _pageTitle = "Chart of Accounts";

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accounts = [];

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private AccountType? _selectedAccountType;

    [ObservableProperty]
    private AccountItemViewModel? _selectedParentAccount;

    public ObservableCollection<AccountItemViewModel> FilteredParentAccounts { get; } = [];

    [ObservableProperty]
    private string _accountCode = string.Empty;

    [ObservableProperty]
    private string _accountName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string ActionButtonText => SelectedAccount == null ? "Add Account" : "Save Changes";

    public AccountType[] AccountTypes { get; } = Enum.GetValues<AccountType>();

    [ObservableProperty]
    private HierarchicalTreeDataGridSource<AccountItemViewModel>? _source;

    public ChartOfAccountsViewModel(
        IFinanceManagementFacade financeFacade,
        IUserSessionService userSessionService,
        IQueryProcessor queryProcessor)
    {
        var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _financeFacade = financeFacade;
        _userSessionService = userSessionService;
        _queryProcessor = queryProcessor;

        InitializeSource();
        LoadAccounts();
        SetupReactiveUpdates(syncContext);
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
                new TextColumn<AccountItemViewModel, string>("Type", x => x.TypeName, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
            }
        };

        var selection = new TreeDataGridRowSelectionModel<AccountItemViewModel>(Source)
        {
            SingleSelect = true
        };

        Source.Selection = selection;

        selection.SelectionChanged += (s, e) =>
        {
            SelectedAccount = selection.SelectedItem;
        };
    }

    private void SetupReactiveUpdates(SynchronizationContext syncContext)
    {
        _financeFacade.CurrentAccounts
            .Merge(_financeFacade.AccountUpdated)
            .Merge(_financeFacade.AccountDeleted)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(syncContext)
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
        var viewModels = accounts.Select(a => new AccountItemViewModel
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            Type = a.Type
        }).ToList();

        var lookup = viewModels.ToDictionary(a => a.Id);
        var roots = new List<AccountItemViewModel>();

        foreach (var accountData in accounts)
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
            SelectedAccountType = value.Type;
            SelectedParentAccount = value.Parent;
        }
        else
        {
            AccountCode = string.Empty;
            AccountName = string.Empty;
            SelectedAccountType = null;
            SelectedParentAccount = null;
        }

        RefreshFilteredParentAccounts();
        OnPropertyChanged(nameof(ActionButtonText));
    }

    partial void OnSelectedAccountTypeChanged(AccountType? value) => RefreshFilteredParentAccounts();

    private void RefreshFilteredParentAccounts()
    {
        FilteredParentAccounts.Clear();
        var allAccounts = GetAllAccounts(Accounts);
        foreach (var account in allAccounts)
        {
            if (CanBeParent(account))
            {
                FilteredParentAccounts.Add(account);
            }
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

    [RelayCommand]
    private async Task AddAccount()
    {
        if (!ValidateAndShow()) return;

        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        try
        {
            if (SelectedAccount != null)
            {
                await _financeFacade.UpdateAccount(
                    session.Organisation.Id,
                    SelectedAccount.Id,
                    AccountCode,
                    AccountName,
                    SelectedAccountType?.ToString() ?? string.Empty,
                    SelectedParentAccount?.Id);
                StatusMessage = "Account updated successfully.";
            }
            else
            {
                await _financeFacade.CreateAccount(
                    session.Organisation.Id,
                    AccountCode,
                    AccountName,
                    SelectedAccountType?.ToString() ?? string.Empty,
                    SelectedParentAccount?.Id);
                StatusMessage = "Account created successfully.";
            }

            ResetForm();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving account: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteAccount()
    {
        if (SelectedAccount == null) return;

        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        try
        {
            await _financeFacade.DeleteAccount(session.Organisation.Id, SelectedAccount.Id);
            StatusMessage = "Account deleted successfully.";
            ResetForm();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting account: {ex.Message}";
        }
    }

    private void ResetForm()
    {
        SelectedAccount = null;
        AccountCode = string.Empty;
        AccountName = string.Empty;
        SelectedAccountType = null;
        SelectedParentAccount = null;
        ResetValidationState();
    }
}