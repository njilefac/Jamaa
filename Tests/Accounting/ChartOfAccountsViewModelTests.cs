using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Finances.Values;
using Jamaa.Application.Finances;
using Jamaa.Application.Shared;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class ChartOfAccountsViewModelTests
{
    private readonly IFinanceManagementFacade _financeFacade = Substitute.For<IFinanceManagementFacade>();
    private readonly IUserSessionService _userSessionService = Substitute.For<IUserSessionService>();
    private readonly IQueryProcessor _queryProcessor = Substitute.For<IQueryProcessor>();

    public ChartOfAccountsViewModelTests()
    {
        var org = new OrganisationData { Id = "org-1", Name = "Test Org" };
        var session = new UserSession(true, "testuser", Guid.NewGuid(), org);
        
        _userSessionService.CurrentUserSession.Returns(session);
        _financeFacade.GetAccounts(Arg.Any<string>()).Returns(Task.FromResult<IList<AccountData>>(new List<AccountData>()));
        
        _financeFacade.CurrentAccounts.Returns(Observable.Empty<AccountData>());
        _financeFacade.AccountUpdated.Returns(Observable.Empty<AccountData>());
        _financeFacade.AccountDeleted.Returns(Observable.Empty<AccountData>());
    }

    [Fact]
    public void SelectedAccount_ShouldPopulateFormFields()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        var account = new AccountItemViewModel
        {
            Id = "acc-1",
            Code = "1000",
            Name = "Cash",
            Type = AccountType.Asset
        };

        viewModel.SelectedAccount = account;

        viewModel.AccountCode.ShouldBe("1000");
        viewModel.AccountName.ShouldBe("Cash");
        viewModel.SelectedAccountType.ShouldBe(AccountType.Asset);
        viewModel.ActionButtonText.ShouldBe("Save Changes");
        viewModel.FormTitle.ShouldBe("Edit Account");
        viewModel.IsEditMode.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAccountCommand_ShouldCallUpdateAccount_WhenAccountIsSelected()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        var account = new AccountItemViewModel
        {
            Id = "acc-1",
            Code = "1000",
            Name = "Cash",
            Type = AccountType.Asset
        };

        viewModel.SelectedAccount = account;
        viewModel.AccountCode = "1000";
        viewModel.AccountName = "Cash Updated";
        viewModel.SelectedAccountType = AccountType.Asset;
        
        _financeFacade.UpdateAccount(
            "org-1", 
            "acc-1", 
            "1000", 
            "Cash Updated", 
            "Asset", 
            null)
            .Returns(Task.CompletedTask);

        await viewModel.AddAccountCommand.ExecuteAsync(null);

        await _financeFacade.Received(1).UpdateAccount(
            "org-1", 
            "acc-1", 
            "1000", 
            "Cash Updated", 
            "Asset", 
            null);
        
        viewModel.StatusMessage.ShouldBe("Account updated successfully.");
        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.AccountName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task AddAccountCommand_ShouldCallCreateAccount_WhenNoAccountIsSelected()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "1100";
        viewModel.AccountName = "Bank";

        _financeFacade.CreateAccount(
            "org-1", 
            "1100", 
            "Bank", 
            "Asset", 
            null)
            .Returns(Task.CompletedTask);

        await viewModel.AddAccountCommand.ExecuteAsync(null);

        await _financeFacade.Received(1).CreateAccount(
            "org-1", 
            "1100", 
            "Bank", 
            "Asset", 
            null);
            
        viewModel.StatusMessage.ShouldBe("Account created successfully.");
    }

    [Fact]
    public void ResetFormCommand_ShouldClearSelectionAndFields()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        viewModel.SelectedAccount = new AccountItemViewModel { Id = "acc-1", Code = "1000", Name = "Cash", Type = AccountType.Asset };
        viewModel.AccountCode = "1000";
        viewModel.AccountName = "Cash";

        viewModel.ResetFormCommand.Execute(null);

        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.AccountCode.ShouldBe(string.Empty);
        viewModel.AccountName.ShouldBe(string.Empty);
        viewModel.FormTitle.ShouldBe("Add New Account");
        viewModel.IsEditMode.ShouldBeFalse();
    }
    [Fact]
    public async Task DeleteAccountCommand_ShouldCallDeleteAccount_AndReloadAccounts()
    {
        // Arrange
        var deletedSubject = new Subject<AccountData>();
        _financeFacade.AccountDeleted.Returns(deletedSubject);
        
        var initialAccount = new AccountData { Id = "acc-1", Code = "1000", Name = "Cash", OrganisationId = "org-1", Type = AccountType.Asset };
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData> { initialAccount }));

        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        // Initial load
        await Task.Delay(10); // Give some time for subscription to trigger if it was started in constructor
        // Wait, LoadAccounts is async void and called in constructor (indirectly maybe? No, let's check)
        
        // Act: Initial load usually happens on initialization or when first subscriber comes. 
        // In this VM it's called in constructor via LoadAccounts().
        
        // Let's force a LoadAccounts to be sure
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod("LoadAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loadMethod.Invoke(viewModel, null);
        await Task.Delay(100); // Wait for async void LoadAccounts

        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.Accounts[0].Id.ShouldBe("acc-1");

        // Prepare for deletion
        var accountItem = viewModel.Accounts[0];
        viewModel.SelectedAccount = accountItem;

        _financeFacade.DeleteAccount("org-1", "acc-1").Returns(Task.CompletedTask);
        // After deletion, GetAccounts will return empty
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData>()));

        // Act: Delete
        await viewModel.DeleteAccountCommand.ExecuteAsync(null);

        // Simulate the reactive update from facade
        deletedSubject.OnNext(initialAccount);
        await Task.Delay(200); // Wait for Throttle and async void LoadAccounts

        // Assert
        await _financeFacade.Received(1).DeleteAccount("org-1", "acc-1");
        viewModel.Accounts.ShouldBeEmpty();
        viewModel.StatusMessage.ShouldBe("Account deleted successfully.");
        viewModel.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void SelectedAccount_WithParent_ShouldPopulateParentAccount()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        var parentAccount = new AccountItemViewModel
        {
            Id = "parent-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };

        var childAccount = new AccountItemViewModel
        {
            Id = "child-1",
            Code = "1010",
            Name = "Cash",
            Type = AccountType.Asset,
            Parent = parentAccount
        };

        // We need to simulate that parentAccount is in the list of available accounts
        // so that it can be selected in the FilteredParentAccounts.
        // But first, let's see if the property itself is set.
        
        viewModel.SelectedAccount = childAccount;

        viewModel.AccountCode.ShouldBe("1010");
        viewModel.AccountName.ShouldBe("Cash");
        viewModel.SelectedAccountType.ShouldBe(AccountType.Asset);
        // It is null initially because parentAccount is not in the tree yet
        viewModel.SelectedParentAccount.ShouldBeNull();
        
        // Check if FilteredParentAccounts contains the parent
        // Actually we need to populate Accounts first for RefreshFilteredParentAccounts to find it
        var accountsField = typeof(ChartOfAccountsViewModel).GetField("_accounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var accountsList = new ObservableCollection<AccountItemViewModel> { parentAccount, childAccount };
        accountsField.SetValue(viewModel, accountsList);
        
        // Trigger refresh
        var refreshMethod = typeof(ChartOfAccountsViewModel).GetMethod("RefreshFilteredParentAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        refreshMethod.Invoke(viewModel, null);

        // Re-select to trigger mapping
        viewModel.SelectedAccount = null;
        viewModel.SelectedAccount = childAccount;

        viewModel.FilteredParentAccounts.ShouldContain(p => p.Id == "parent-1");
        
        // Important: SelectedParentAccount MUST be the same reference as the one in FilteredParentAccounts
        // for the Avalonia ComboBox to show it as selected.
        var parentInList = viewModel.FilteredParentAccounts.First(p => p.Id == "parent-1");
        viewModel.SelectedParentAccount.ShouldBeSameAs(parentInList);
    }

    [Fact]
    public void SelectedAccount_WithParent_ShouldPopulateParentAccount_EvenIfReferenceIsDifferent()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        var parentAccountInTree = new AccountItemViewModel
        {
            Id = "parent-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };

        var parentAccountFromSelected = new AccountItemViewModel
        {
            Id = "parent-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };

        var childAccount = new AccountItemViewModel
        {
            Id = "child-1",
            Code = "1010",
            Name = "Cash",
            Type = AccountType.Asset,
            Parent = parentAccountFromSelected
        };

        // Populate the tree with parentAccountInTree
        var accountsField = typeof(ChartOfAccountsViewModel).GetField("_accounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var accountsList = new ObservableCollection<AccountItemViewModel> { parentAccountInTree };
        accountsField.SetValue(viewModel, accountsList);
        
        // Select the child account
        viewModel.SelectedAccount = childAccount;

        // Verify that SelectedParentAccount was mapped to the reference in the list
        viewModel.SelectedParentAccount.ShouldNotBeNull();
        viewModel.SelectedParentAccount.Id.ShouldBe("parent-1");
        viewModel.SelectedParentAccount.ShouldBeSameAs(parentAccountInTree);
        viewModel.SelectedParentAccount.ShouldNotBeSameAs(parentAccountFromSelected);
    }
    [Fact]
    public void SelectedAccountType_Change_ShouldSuggestCode()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode.ShouldBe("1000");

        viewModel.SelectedAccountType = AccountType.Liability;
        viewModel.AccountCode.ShouldBe("2000");

        viewModel.SelectedAccountType = AccountType.Equity;
        viewModel.AccountCode.ShouldBe("3000");

        viewModel.SelectedAccountType = AccountType.Revenue;
        viewModel.AccountCode.ShouldBe("4000");

        viewModel.SelectedAccountType = AccountType.Expense;
        viewModel.AccountCode.ShouldBe("5000");

        viewModel.AccountName = "Admin Fees";
        viewModel.AccountCode.ShouldBe("6000");
    }

    [Fact]
    public void InvalidAccountCode_ShouldHaveValidationError()
    {
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "2000"; // Invalid for Asset

        viewModel.HasErrors.ShouldBeTrue();
        var errors = viewModel.GetErrors(nameof(viewModel.AccountCode)).Cast<ValidationResult>().ToList();
        errors.ShouldNotBeEmpty();
        errors[0].ErrorMessage.ShouldContain("Code for Asset must be between 1000 and 1999");
    }

    [Fact]
    public void SuggestAccountCode_ShouldUseNextAvailableCode()
    {
        var accounts = new List<AccountData>
        {
            new AccountData { Id = "1", Code = "1000", Type = AccountType.Asset, OrganisationId = "org-1", Name = "A" },
            new AccountData { Id = "2", Code = "1005", Type = AccountType.Asset, OrganisationId = "org-1", Name = "B" }
        };
        _financeFacade.GetAccounts(Arg.Any<string>()).Returns(Task.FromResult<IList<AccountData>>(accounts));
        
        var viewModel = new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor);
        // Force load
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod("LoadAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loadMethod.Invoke(viewModel, null);

        viewModel.SelectedAccountType = AccountType.Asset;
        
        viewModel.AccountCode.ShouldBe("1006");
    }
}
