using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using Jamaa.Desktop.Services.Notifications;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class ChartOfAccountsViewModelTests
{
    private readonly IFinanceManagementFacade _financeFacade = Substitute.For<IFinanceManagementFacade>();
    private readonly IUserSessionService _userSessionService = Substitute.For<IUserSessionService>();
    private readonly IQueryProcessor _queryProcessor = Substitute.For<IQueryProcessor>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();

    public ChartOfAccountsViewModelTests()
    {
        var org = new OrganisationData { Id = "org-1", Name = "Test Org" };
        var session = new UserSession(true, "testuser", Guid.NewGuid(), org);
        
        _userSessionService.CurrentUserSession.Returns(session);
        _financeFacade.GetAccounts(Arg.Any<string>()).Returns(Task.FromResult<IList<AccountData>>(new List<AccountData>()));
        
        _financeFacade.AccountCreated.Returns(Observable.Empty<AccountData>());
        _financeFacade.AccountUpdated.Returns(Observable.Empty<AccountData>());
        _financeFacade.AccountDeleted.Returns(Observable.Empty<AccountData>());
    }

    private ChartOfAccountsViewModel CreateViewModel() =>
        new ChartOfAccountsViewModel(_financeFacade, _userSessionService, _queryProcessor, _notificationService);

    [Fact]
    public void SelectedAccount_ShouldPopulateFormFields()
    {
        var viewModel = CreateViewModel();
        var account = new AccountItemViewModel
        {
            Id = "acc-1",
            Code = "1000",
            Name = "Cash",
            Description = "Main cash account",
            Type = AccountType.Asset
        };

        viewModel.SelectedAccount = account;

        viewModel.AccountCode.ShouldBe("1000");
        viewModel.AccountName.ShouldBe("Cash");
        viewModel.AccountDescription.ShouldBe("Main cash account");
        viewModel.SelectedAccountType.ShouldBe(AccountType.Asset);
        viewModel.ActionButtonText.ShouldBe("Save Changes");
        viewModel.FormTitle.ShouldBe("Edit Account");
        viewModel.IsEditMode.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAccountCommand_ShouldCallUpdateAccount_WhenAccountIsSelected()
    {
        // Arrange – use a ReplaySubject so TrackOperationAsync receives the event
        // even if it subscribes slightly after the emission
        var updatedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountUpdated.Returns(updatedSubject);

        _financeFacade.UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", "Updated description", AccountType.Asset, null)
            .Returns(_ =>
            {
                updatedSubject.OnNext(new AccountData { Id = "acc-1", OrganisationId = "org-1", Code = "1000", Name = "Cash Updated", Description = "Updated description", Type = AccountType.Asset });
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        var account = new AccountItemViewModel { Id = "acc-1", Code = "1000", Name = "Cash", Type = AccountType.Asset };

        viewModel.SelectedAccount = account;
        viewModel.AccountCode = "1000";
        viewModel.AccountName = "Cash Updated";
        viewModel.AccountDescription = "Updated description";
        viewModel.SelectedAccountType = AccountType.Asset;

        // Act
        await viewModel.AddAccountCommand.ExecuteAsync(null);

        // Assert – facade was called
        await _financeFacade.Received(1).UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", "Updated description", AccountType.Asset, null);
        // Assert – success notification was shown
        _notificationService.Received(1).Show(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Saved") || s.Contains("Cash Updated")),
            NotificationType.Success);
        // Assert – form was reset after confirmation
        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.AccountName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task AddAccountCommand_ShouldCallCreateAccount_WhenNoAccountIsSelected()
    {
        // Arrange – AccountCreated is the confirmation observable for create
        var createdSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountCreated.Returns(createdSubject);
        _financeFacade.AccountUpdated.Returns(Observable.Empty<AccountData>());
        _financeFacade.AccountDeleted.Returns(Observable.Empty<AccountData>());

        _financeFacade.CreateAccount("org-1", "1100", "Bank", "Primary bank account", AccountType.Asset, null)
            .Returns(_ =>
            {
                createdSubject.OnNext(new AccountData { Id = "new-1", OrganisationId = "org-1", Code = "1100", Name = "Bank", Description = "Primary bank account", Type = AccountType.Asset });
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "1100";
        viewModel.AccountName = "Bank";
        viewModel.AccountDescription = "Primary bank account";

        // Act
        await viewModel.AddAccountCommand.ExecuteAsync(null);

        // Assert – facade was called
        await _financeFacade.Received(1).CreateAccount("org-1", "1100", "Bank", "Primary bank account", AccountType.Asset, null);
        // Assert – success notification was shown
        _notificationService.Received(1).Show(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Created") || s.Contains("Bank")),
            NotificationType.Success);
        viewModel.StatusMessage.ShouldContain("confirmed from event stream");
    }

    [Fact]
    public async Task AddAccountCommand_ShouldReloadTreeFromReadModel_WhenCreatedAccountIsChildOfExistingParent()
    {
        var createdSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountCreated.Returns(createdSubject);

        var parentAccount = new AccountData
        {
            Id = "parent-1",
            OrganisationId = "org-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };

        var childAccount = new AccountData
        {
            Id = "child-1",
            OrganisationId = "org-1",
            Code = "1010",
            Name = "Cash",
            Type = AccountType.Asset,
            ParentId = "parent-1"
        };

        _financeFacade.GetAccounts("org-1").Returns(
            Task.FromResult<IList<AccountData>>(new List<AccountData> { parentAccount }),
            Task.FromResult<IList<AccountData>>(new List<AccountData> { parentAccount, childAccount }));

        _financeFacade.CreateAccount("org-1", "1010", "Cash", "Cash on hand", AccountType.Asset, "parent-1")
            .Returns(_ =>
            {
                createdSubject.OnNext(childAccount);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "the parent account to load");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "1010";
        viewModel.AccountName = "Cash";
        viewModel.AccountDescription = "Cash on hand";
        viewModel.SelectedParentAccount = viewModel.FilteredParentAccounts.Single(account => account.Id == "parent-1");

        await viewModel.AddAccountCommand.ExecuteAsync(null);
        await WaitUntilAsync(() =>
            viewModel.Accounts.Count == 1 &&
            viewModel.Accounts[0].SubAccounts.Count == 1,
            "the created child account to appear under the parent");

        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.Accounts[0].Id.ShouldBe("parent-1");
        viewModel.Accounts[0].SubAccounts.Count.ShouldBe(1);
        viewModel.Accounts[0].SubAccounts[0].Id.ShouldBe("child-1");
        viewModel.Accounts[0].SubAccounts[0].Parent.ShouldBeSameAs(viewModel.Accounts[0]);
    }

    [Fact]
    public void ResetFormCommand_ShouldClearSelectionAndFields()
    {
        var viewModel = CreateViewModel();
        viewModel.SelectedAccount = new AccountItemViewModel { Id = "acc-1", Code = "1000", Name = "Cash", Type = AccountType.Asset };
        viewModel.AccountCode = "1000";
        viewModel.AccountName = "Cash";
        viewModel.AccountDescription = "Cash reserve";

        viewModel.ResetFormCommand.Execute(null);

        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.AccountCode.ShouldBe(string.Empty);
        viewModel.AccountName.ShouldBe(string.Empty);
        viewModel.AccountDescription.ShouldBe(string.Empty);
        viewModel.FormTitle.ShouldBe("Add New Account");
        viewModel.IsEditMode.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountCommand_ShouldCallDeleteAccount_AndReloadAccounts()
    {
        // Arrange
        var deletedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountDeleted.Returns(deletedSubject);
        
        var initialAccount = new AccountData { Id = "acc-1", Code = "1000", Name = "Cash", OrganisationId = "org-1", Type = AccountType.Asset };
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData> { initialAccount }));

        _financeFacade.DeleteAccount("org-1", "acc-1")
            .Returns(_ =>
            {
                deletedSubject.OnNext(initialAccount);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "the initial account to load");

        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.SelectedAccount = viewModel.Accounts[0];

        // After deletion GetAccounts returns empty
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData>()));

        // Act
        await viewModel.DeleteAccountCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 0, "the deleted account to be removed from the tree");

        // Assert
        await _financeFacade.Received(1).DeleteAccount("org-1", "acc-1");
        _notificationService.Received(1).Show(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Deleted") || s.Contains("Cash")),
            NotificationType.Success);
        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAccountCommand_ShouldRemoveChildFromUi_WhenDeleteEventArrivesAfterProjectionCatchUp()
    {
        var deletedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountDeleted.Returns(deletedSubject);

        var parent = new AccountData
        {
            Id = "parent-1",
            Code = "1000",
            Name = "Assets",
            OrganisationId = "org-1",
            Type = AccountType.Asset
        };

        var child = new AccountData
        {
            Id = "child-1",
            Code = "1010",
            Name = "Cash",
            OrganisationId = "org-1",
            Type = AccountType.Asset,
            ParentId = "parent-1"
        };

        var readModelAccounts = new List<AccountData> { parent, child };
        _financeFacade.GetAccounts("org-1")
            .Returns(_ => Task.FromResult<IList<AccountData>>(readModelAccounts.ToList()));

        _financeFacade.DeleteAccount("org-1", "child-1")
            .Returns(_ =>
            {
                readModelAccounts.RemoveAll(account => account.Id == child.Id);
                deletedSubject.OnNext(child);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() =>
            viewModel.Accounts.Count == 1 &&
            viewModel.Accounts[0].SubAccounts.Count == 1,
            "the parent and child accounts to load");

        var selectedParent = viewModel.Accounts.Single(account => account.Id == "parent-1");
        var selectedChild = selectedParent.SubAccounts.Single(account => account.Id == "child-1");
        viewModel.SelectedAccount = selectedChild;

        await viewModel.DeleteAccountCommand.ExecuteAsync(null);
        await WaitUntilAsync(() =>
            viewModel.Accounts.Count == 1 &&
            viewModel.Accounts[0].SubAccounts.Count == 0,
            "the deleted child account to disappear from the parent");

        await _financeFacade.Received(1).DeleteAccount("org-1", "child-1");
        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.Accounts[0].Id.ShouldBe("parent-1");
        viewModel.Accounts[0].SubAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAccountCommand_ShouldSucceed_WhenCreateEventIsMissing_ButReadModelShowsAccount()
    {
        var deletedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountDeleted.Returns(deletedSubject);
        _financeFacade.AccountCreated.Returns(Observable.Empty<AccountData>());

        var initialAccount = new AccountData
        {
            Id = "acc-1",
            Code = "1000",
            Name = "Cash",
            OrganisationId = "org-1",
            Type = AccountType.Asset
        };

        var createdAccount = new AccountData
        {
            Id = "acc-2",
            Code = "1001",
            Name = "Bank",
            Description = "Primary bank account",
            OrganisationId = "org-1",
            Type = AccountType.Asset
        };

        var readModelAccounts = new List<AccountData> { initialAccount };
        _financeFacade.GetAccounts("org-1")
            .Returns(_ => Task.FromResult<IList<AccountData>>(readModelAccounts.ToList()));

        _financeFacade.DeleteAccount("org-1", "acc-1")
            .Returns(_ =>
            {
                readModelAccounts.RemoveAll(account => account.Id == "acc-1");
                deletedSubject.OnNext(initialAccount);
                return Task.CompletedTask;
            });

        _financeFacade.CreateAccount("org-1", "1001", "Bank", "Primary bank account", AccountType.Asset, null)
            .Returns(_ =>
            {
                readModelAccounts.Add(createdAccount);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "the initial account to load before deletion");

        viewModel.SelectedAccount = viewModel.Accounts.Single(account => account.Id == "acc-1");
        await viewModel.DeleteAccountCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 0, "the deleted account to disappear before recreating");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "1001";
        viewModel.AccountName = "Bank";
        viewModel.AccountDescription = "Primary bank account";

        await viewModel.AddAccountCommand.ExecuteAsync(null);

        await _financeFacade.Received(1).CreateAccount("org-1", "1001", "Bank", "Primary bank account", AccountType.Asset, null);
        _notificationService.Received().Show(
            Arg.Any<string>(),
            Arg.Is<string>(message => message.Contains("Created", StringComparison.OrdinalIgnoreCase) && message.Contains("Bank", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Success);
        viewModel.StatusMessage.ShouldContain("confirmed from read model");
    }

    [Fact]
    public void DeleteAccountCommand_ShouldBeDisabled_WhenSelectedAccountHasChildren()
    {
        var viewModel = CreateViewModel();
        var parent = new AccountItemViewModel
        {
            Id = "parent-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };
        parent.SubAccounts.Add(new AccountItemViewModel
        {
            Id = "child-1",
            Code = "1010",
            Name = "Cash",
            Type = AccountType.Asset,
            Parent = parent
        });

        viewModel.SelectedAccount = parent;

        viewModel.DeleteAccountCommand.CanExecute(null).ShouldBeFalse();
        viewModel.DeleteAccountTooltip.ShouldBe("This account cannot be deleted because it has child accounts.");
    }

    [Fact]
    public void DeleteAccountCommand_ShouldBeEnabled_ForLeafAccount()
    {
        var viewModel = CreateViewModel();
        var leaf = new AccountItemViewModel
        {
            Id = "leaf-1",
            Code = "1001",
            Name = "Petty Cash",
            Type = AccountType.Asset
        };

        viewModel.SelectedAccount = leaf;

        viewModel.DeleteAccountCommand.CanExecute(null).ShouldBeTrue();
        viewModel.DeleteAccountTooltip.ShouldBe("Permanently delete this account");
    }

    [Fact]
    public void SelectedAccount_WithParent_ShouldPopulateParentAccount()
    {
        var viewModel = CreateViewModel();
        
        var parentAccount = new AccountItemViewModel { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var childAccount = new AccountItemViewModel { Id = "child-1", Code = "1010", Name = "Cash", Type = AccountType.Asset, Parent = parentAccount };
        
        viewModel.SelectedAccount = childAccount;

        viewModel.AccountCode.ShouldBe("1010");
        viewModel.AccountName.ShouldBe("Cash");
        viewModel.SelectedAccountType.ShouldBe(AccountType.Asset);
        viewModel.SelectedParentAccount.ShouldBeNull(); // parent not in tree yet
        
        var accountsField = typeof(ChartOfAccountsViewModel).GetField("_accounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        accountsField!.SetValue(viewModel, new ObservableCollection<AccountItemViewModel> { parentAccount, childAccount });
        
        var refreshMethod = typeof(ChartOfAccountsViewModel).GetMethod("RefreshFilteredParentAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        refreshMethod!.Invoke(viewModel, null);

        viewModel.SelectedAccount = null;
        viewModel.SelectedAccount = childAccount;

        viewModel.FilteredParentAccounts.ShouldContain(p => p.Id == "parent-1");
        
        var parentInList = viewModel.FilteredParentAccounts.First(p => p.Id == "parent-1");
        viewModel.SelectedParentAccount.ShouldBeSameAs(parentInList);
    }

    [Fact]
    public void SelectedAccount_WithParent_ShouldPopulateParentAccount_EvenIfReferenceIsDifferent()
    {
        var viewModel = CreateViewModel();
        
        var parentAccountInTree = new AccountItemViewModel { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var parentAccountFromSelected = new AccountItemViewModel { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var childAccount = new AccountItemViewModel { Id = "child-1", Code = "1010", Name = "Cash", Type = AccountType.Asset, Parent = parentAccountFromSelected };

        var accountsField = typeof(ChartOfAccountsViewModel).GetField("_accounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        accountsField!.SetValue(viewModel, new ObservableCollection<AccountItemViewModel> { parentAccountInTree });
        
        viewModel.SelectedAccount = childAccount;

        viewModel.SelectedParentAccount.ShouldNotBeNull();
        viewModel.SelectedParentAccount!.Id.ShouldBe("parent-1");
        viewModel.SelectedParentAccount.ShouldBeSameAs(parentAccountInTree);
        viewModel.SelectedParentAccount.ShouldNotBeSameAs(parentAccountFromSelected);
    }

    [Fact]
    public void SelectedAccountType_Change_ShouldSuggestCode()
    {
        var viewModel = CreateViewModel();
        
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
        var viewModel = CreateViewModel();
        
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "2000"; // Invalid for Asset

        viewModel.HasErrors.ShouldBeTrue();
        var errorMessages = viewModel.GetErrors(nameof(viewModel.AccountCode))
            .Select(error => error.ToString())
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();

        errorMessages.ShouldNotBeEmpty();
        errorMessages[0].ShouldContain("Code for Asset must be between 1000 and 1999");
    }

    [Fact]
    public async Task SuggestAccountCode_ShouldUseNextAvailableCode()
    {
        var accounts = new List<AccountData>
        {
            new AccountData { Id = "1", Code = "1000", Type = AccountType.Asset, OrganisationId = "org-1", Name = "A" },
            new AccountData { Id = "2", Code = "1005", Type = AccountType.Asset, OrganisationId = "org-1", Name = "B" }
        };
        _financeFacade.GetAccounts(Arg.Any<string>()).Returns(Task.FromResult<IList<AccountData>>(accounts));
        
        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 2, "existing accounts to load before suggesting a code");

        viewModel.SelectedAccountType = AccountType.Asset;
        
        viewModel.AccountCode.ShouldBe("1006");
    }

    private static async Task InvokePrivateLoadAccountsAsync(ChartOfAccountsViewModel viewModel)
    {
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod(
            "LoadAccounts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        loadMethod.ShouldNotBeNull();
        loadMethod.Invoke(viewModel, null);
        await Task.Yield();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string expectation, int timeoutMilliseconds = 1500)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        condition().ShouldBeTrue($"Timed out waiting for {expectation}.");
    }
}
