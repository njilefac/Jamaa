using System;
using System.Collections.Generic;
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
        
        viewModel.AccountCode = "1100";
        viewModel.AccountName = "Bank";
        viewModel.SelectedAccountType = AccountType.Asset;

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
}
