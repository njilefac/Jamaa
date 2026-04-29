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
        // Arrange – use a ReplaySubject so TrackOperationAsync receives the event
        // even if it subscribes slightly after the emission
        var updatedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountUpdated.Returns(updatedSubject);

        _financeFacade.UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", string.Empty, AccountType.Asset, null)
            .Returns(ci =>
            {
                updatedSubject.OnNext(new AccountData { Id = "acc-1", OrganisationId = "org-1", Code = "1000", Name = "Cash Updated", Type = AccountType.Asset });
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        var account = new AccountItemViewModel { Id = "acc-1", Code = "1000", Name = "Cash", Type = AccountType.Asset };

        viewModel.SelectedAccount = account;
        viewModel.AccountCode = "1000";
        viewModel.AccountName = "Cash Updated";
        viewModel.SelectedAccountType = AccountType.Asset;

        // Act
        await viewModel.AddAccountCommand.ExecuteAsync(null);

        // Assert – facade was called
        await _financeFacade.Received(1).UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", string.Empty, AccountType.Asset, null);
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

        _financeFacade.CreateAccount("org-1", "1100", "Bank", string.Empty, AccountType.Asset, null)
            .Returns(ci =>
            {
                createdSubject.OnNext(new AccountData { Id = "new-1", OrganisationId = "org-1", Code = "1100", Name = "Bank", Type = AccountType.Asset });
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode = "1100";
        viewModel.AccountName = "Bank";

        // Act
        await viewModel.AddAccountCommand.ExecuteAsync(null);

        // Assert – facade was called
        await _financeFacade.Received(1).CreateAccount("org-1", "1100", "Bank", string.Empty, AccountType.Asset, null);
        // Assert – success notification was shown
        _notificationService.Received(1).Show(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Created") || s.Contains("Bank")),
            NotificationType.Success);
    }

    [Fact]
    public void ResetFormCommand_ShouldClearSelectionAndFields()
    {
        var viewModel = CreateViewModel();
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
        var deletedSubject = new ReplaySubject<AccountData>(1);
        _financeFacade.AccountDeleted.Returns(deletedSubject);
        
        var initialAccount = new AccountData { Id = "acc-1", Code = "1000", Name = "Cash", OrganisationId = "org-1", Type = AccountType.Asset };
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData> { initialAccount }));

        _financeFacade.DeleteAccount("org-1", "acc-1")
            .Returns(ci =>
            {
                deletedSubject.OnNext(initialAccount);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        
        // Force initial load
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod("LoadAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loadMethod!.Invoke(viewModel, null);
        await Task.Delay(100);

        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.SelectedAccount = viewModel.Accounts[0];

        // After deletion GetAccounts returns empty
        _financeFacade.GetAccounts("org-1").Returns(Task.FromResult<IList<AccountData>>(new List<AccountData>()));

        // Act
        await viewModel.DeleteAccountCommand.ExecuteAsync(null);
        await Task.Delay(200); // Wait for Throttle + async void LoadAccounts

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
        
        var viewModel = CreateViewModel();
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod("LoadAccounts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loadMethod!.Invoke(viewModel, null);

        viewModel.SelectedAccountType = AccountType.Asset;
        
        viewModel.AccountCode.ShouldBe("1006");
    }
}
