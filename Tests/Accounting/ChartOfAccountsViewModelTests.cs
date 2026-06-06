using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Domain.Accounting.Values;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Notifications;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class ChartOfAccountsViewModelTests
{
    private readonly IAccountingFacade _accountFacade = Substitute.For<IAccountingFacade>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IUserSessionService _userSessionService = Substitute.For<IUserSessionService>();

    public ChartOfAccountsViewModelTests()
    {
        var org = new OrganisationData { Id = "org-1", Name = "Test Org" };
        var session = new UserSession(true, "testuser", Guid.NewGuid(), org);

        _userSessionService.CurrentUserSession.Returns(session);
        _accountFacade.GetChartOfAccounts(Arg.Any<string>())
            .Returns(orgId => Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = new List<AccountData>() }));
        _accountFacade.GetAccountingSettings(Arg.Any<string>())
            .Returns(orgId => Task.FromResult<AccountingSettingsData?>(new AccountingSettingsData
            {
                OrganisationId = orgId.ArgAt<string>(0),
                BaseCurrency = "USD",
                DateFormat = "yyyy-MM-dd",
                DecimalPrecision = 2,
                ThousandSeparator = ",",
                AvailableCurrencies =
                [
                    new AccountingAvailableCurrencyData
                    {
                        OrganisationId = orgId.ArgAt<string>(0),
                        CurrencyCode = "USD",
                        CurrencySymbol = "$"
                    }
                ]
            }));
        _accountFacade.GetFiscalCalendar(Arg.Any<string>())
            .Returns(orgId => Task.FromResult(new FiscalCalendarData
            {
                OrganisationId = orgId.ArgAt<string>(0),
                FiscalYears =
                [
                    new FiscalYearData
                    {
                        Id = "fy-2025",
                        OrganisationId = orgId.ArgAt<string>(0),
                        StartDate = new DateTime(2025, 1, 1),
                        EndDate = new DateTime(2025, 12, 31),
                        IsLocked = false,
                        Periods =
                        [
                            new AccountingPeriodData
                            {
                                Id = "p-2025-01",
                                FiscalYearId = "fy-2025",
                                OrganisationId = orgId.ArgAt<string>(0),
                                SequenceNumber = 1,
                                IsLocked = false
                            }
                        ]
                    }
                ]
            }));
        _accountFacade.GetAccountOpeningBalance(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(0m));

        _accountFacade.AccountCreated.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountUpdated.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountDeleted.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountDeactivated.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountReactivated.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountOpeningBalanceSet.Returns(Observable.Empty<AccountingPeriodBalanceData>());
    }

    private ChartOfAccountsViewModel CreateViewModel()
    {
        return new ChartOfAccountsViewModel(_accountFacade, _userSessionService, _notificationService);
    }

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
        _accountFacade.AccountUpdated.Returns(updatedSubject);

        _accountFacade.UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", "Updated description", AccountType.Asset,
                null)
            .Returns(_ =>
            {
                updatedSubject.OnNext(new AccountData
                {
                    Id = "acc-1", OrganisationId = "org-1", Code = "1000", Name = "Cash Updated",
                    Description = "Updated description", Type = AccountType.Asset
                });
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
        await _accountFacade.Received(1).UpdateAccount("org-1", "acc-1", "1000", "Cash Updated", "Updated description",
            AccountType.Asset, null);
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
        _accountFacade.AccountCreated.Returns(createdSubject);
        _accountFacade.AccountUpdated.Returns(Observable.Empty<AccountData>());
        _accountFacade.AccountDeleted.Returns(Observable.Empty<AccountData>());

        _accountFacade.CreateAccount("org-1", "1100", "Bank", "Primary bank account", AccountType.Asset, null)
            .Returns(_ =>
            {
                createdSubject.OnNext(new AccountData
                {
                    Id = "new-1", OrganisationId = "org-1", Code = "1100", Name = "Bank",
                    Description = "Primary bank account", Type = AccountType.Asset
                });
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
        await _accountFacade.Received(1)
            .CreateAccount("org-1", "1100", "Bank", "Primary bank account", AccountType.Asset, null);
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
        _accountFacade.AccountCreated.Returns(createdSubject);

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

        _accountFacade.GetChartOfAccounts("org-1").Returns(
            Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = new List<AccountData> { parentAccount } }),
            Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = new List<AccountData> { parentAccount, childAccount } }));

        _accountFacade.CreateAccount("org-1", "1010", "Cash", "Cash on hand", AccountType.Asset, "parent-1")
            .Returns(_ =>
            {
                createdSubject.OnNext(childAccount);
                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "the parent account to load");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.SelectedParentAccount = viewModel.FilteredParentAccounts.Single(account => account.Id == "parent-1");
        viewModel.AccountCode = "1010";
        viewModel.AccountName = "Cash";
        viewModel.AccountDescription = "Cash on hand";

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
        viewModel.SelectedAccount = new AccountItemViewModel
            { Id = "acc-1", Code = "1000", Name = "Cash", Type = AccountType.Asset };
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
        _accountFacade.AccountDeleted.Returns(deletedSubject);

        var initialAccount = new AccountData
            { Id = "acc-1", Code = "1000", Name = "Cash", OrganisationId = "org-1", Type = AccountType.Asset };
        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = new List<AccountData> { initialAccount } }));

        _accountFacade.DeleteAccount("org-1", "acc-1")
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

        // After deletion GetChartOfAccounts returns empty
        _accountFacade.GetChartOfAccounts("org-1").Returns(Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = new List<AccountData>() }));

        // Act
        await viewModel.DeleteAccountCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 0, "the deleted account to be removed from the tree");

        // Assert
        await _accountFacade.Received(1).DeleteAccount("org-1", "acc-1");
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
        _accountFacade.AccountDeleted.Returns(deletedSubject);

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
        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(_ => Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = readModelAccounts.ToList() }));

        _accountFacade.DeleteAccount("org-1", "child-1")
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

        await _accountFacade.Received(1).DeleteAccount("org-1", "child-1");
        viewModel.Accounts.Count.ShouldBe(1);
        viewModel.Accounts[0].Id.ShouldBe("parent-1");
        viewModel.Accounts[0].SubAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToggleActive_ShouldConfirmFromReadModel_WhenDeactivateEventIsMissing()
    {
        _accountFacade.AccountDeactivated.Returns(Observable.Empty<AccountData>());

        var readModelAccounts = new List<AccountData>
        {
            new()
            {
                Id = "acc-1",
                Code = "1000",
                Name = "Cash",
                OrganisationId = "org-1",
                Type = AccountType.Asset,
                IsActive = true
            }
        };

        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(_ => Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = readModelAccounts.ToList() }));

        _accountFacade.DeactivateAccount("org-1", "acc-1")
            .Returns(_ =>
            {
                readModelAccounts[0] = new AccountData
                {
                    Id = "acc-1",
                    Code = "1000",
                    Name = "Cash",
                    OrganisationId = "org-1",
                    Type = AccountType.Asset,
                    IsActive = false
                };

                return Task.CompletedTask;
            });

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "the account to load before deactivation");

        var item = viewModel.Accounts.Single(account => account.Id == "acc-1");
        await InvokePrivateToggleAccountActiveAsync(viewModel, item);

        await _accountFacade.Received(1).DeactivateAccount("org-1", "acc-1");
        _notificationService.Received().Show(
            "Account",
            Arg.Is<string>(message => message.Contains("Deactivated", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Success);
        _notificationService.DidNotReceive().Show(
            "Timeout",
            Arg.Any<string>(),
            NotificationType.Warning);
    }

    [Fact]
    public async Task AddAccountCommand_ShouldSucceed_WhenCreateEventIsMissing_ButReadModelShowsAccount()
    {
        var deletedSubject = new ReplaySubject<AccountData>(1);
        _accountFacade.AccountDeleted.Returns(deletedSubject);
        _accountFacade.AccountCreated.Returns(Observable.Empty<AccountData>());

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
        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(_ => Task.FromResult(new ChartOfAccountsData { OrganisationId = "org-1", Accounts = readModelAccounts.ToList() }));

        _accountFacade.DeleteAccount("org-1", "acc-1")
            .Returns(_ =>
            {
                readModelAccounts.RemoveAll(account => account.Id == "acc-1");
                deletedSubject.OnNext(initialAccount);
                return Task.CompletedTask;
            });

        _accountFacade.CreateAccount("org-1", "1001", "Bank", "Primary bank account", AccountType.Asset, null)
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

        await _accountFacade.Received(1)
            .CreateAccount("org-1", "1001", "Bank", "Primary bank account", AccountType.Asset, null);
        _notificationService.Received().Show(
            Arg.Any<string>(),
            Arg.Is<string>(message => message.Contains("Created", StringComparison.OrdinalIgnoreCase) &&
                                      message.Contains("Bank", StringComparison.OrdinalIgnoreCase)),
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

        var parentAccount = new AccountItemViewModel
            { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var childAccount = new AccountItemViewModel
            { Id = "child-1", Code = "1010", Name = "Cash", Type = AccountType.Asset, Parent = parentAccount };

        viewModel.SelectedAccount = childAccount;

        viewModel.AccountCode.ShouldBe("1010");
        viewModel.AccountName.ShouldBe("Cash");
        viewModel.SelectedAccountType.ShouldBe(AccountType.Asset);
        viewModel.SelectedParentAccount.ShouldBeNull(); // parent not in tree yet

        var accountsField =
            typeof(ChartOfAccountsViewModel).GetField("_accounts", BindingFlags.NonPublic | BindingFlags.Instance);
        accountsField!.SetValue(viewModel,
            new ObservableCollection<AccountItemViewModel> { parentAccount, childAccount });

        var refreshMethod = typeof(ChartOfAccountsViewModel).GetMethod("RefreshFilteredParentAccounts",
            BindingFlags.NonPublic | BindingFlags.Instance);
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

        var parentAccountInTree = new AccountItemViewModel
            { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var parentAccountFromSelected = new AccountItemViewModel
            { Id = "parent-1", Code = "1000", Name = "Assets", Type = AccountType.Asset };
        var childAccount = new AccountItemViewModel
        {
            Id = "child-1", Code = "1010", Name = "Cash", Type = AccountType.Asset, Parent = parentAccountFromSelected
        };

        var accountsField =
            typeof(ChartOfAccountsViewModel).GetField("_accounts", BindingFlags.NonPublic | BindingFlags.Instance);
        accountsField!.SetValue(viewModel, new ObservableCollection<AccountItemViewModel> { parentAccountInTree });

        viewModel.SelectedAccount = childAccount;

        viewModel.SelectedParentAccount.ShouldNotBeNull();
        viewModel.SelectedParentAccount!.Id.ShouldBe("parent-1");
        viewModel.SelectedParentAccount.ShouldBeSameAs(parentAccountInTree);
        viewModel.SelectedParentAccount.ShouldNotBeSameAs(parentAccountFromSelected);
    }

    [Fact]
    public void RefreshFilteredParentAccounts_ShouldExcludeInactiveAccounts()
    {
        var viewModel = CreateViewModel();

        var activeParent = new AccountItemViewModel
            { Id = "parent-active", Code = "1000", Name = "Assets", Type = AccountType.Asset, IsActive = true };
        var inactiveParent = new AccountItemViewModel
            { Id = "parent-inactive", Code = "1100", Name = "Legacy Assets", Type = AccountType.Asset, IsActive = false };
        var childAccount = new AccountItemViewModel
            { Id = "child-1", Code = "1010", Name = "Cash", Type = AccountType.Asset, Parent = activeParent };

        var accountsField =
            typeof(ChartOfAccountsViewModel).GetField("_accounts", BindingFlags.NonPublic | BindingFlags.Instance);
        accountsField!.SetValue(viewModel, new ObservableCollection<AccountItemViewModel> { activeParent, inactiveParent, childAccount });

        var refreshMethod = typeof(ChartOfAccountsViewModel).GetMethod("RefreshFilteredParentAccounts",
            BindingFlags.NonPublic | BindingFlags.Instance);
        refreshMethod!.Invoke(viewModel, null);

        viewModel.SelectedAccountType = AccountType.Asset;

        viewModel.FilteredParentAccounts.ShouldContain(account => account.Id == "parent-active");
        viewModel.FilteredParentAccounts.ShouldNotContain(account => account.Id == "parent-inactive");
    }

    [Fact]
    public async Task SelectedParentAccount_ShouldAllowClearingBackToBlank()
    {
        var parentAccount = new AccountData
        {
            Id = "parent-1",
            OrganisationId = "org-1",
            Code = "1100",
            Name = "Assets",
            Type = AccountType.Asset
        };

        _accountFacade.GetChartOfAccounts("org-1").Returns(Task.FromResult(new ChartOfAccountsData
            { OrganisationId = "org-1", Accounts = [parentAccount] }));

        var viewModel = CreateViewModel();
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "parent account to load");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.SelectedParentAccount = viewModel.FilteredParentAccounts.Single(account => account.Id == "parent-1");
        viewModel.SelectedParentAccount.ShouldNotBeNull();

        var blankOption = viewModel.FilteredParentAccounts.First(account =>
            string.IsNullOrEmpty(account.Id) && string.IsNullOrEmpty(account.Name));
        viewModel.SelectedParentAccount = blankOption;

        viewModel.SelectedParentAccount.ShouldBeNull();
    }

    [Fact]
    public void SelectedAccountType_Change_ShouldSuggestCode()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.AccountCode.ShouldBe("1100");

        viewModel.SelectedAccountType = AccountType.Liability;
        viewModel.AccountCode.ShouldBe("2100");

        viewModel.SelectedAccountType = AccountType.Equity;
        viewModel.AccountCode.ShouldBe("3100");

        viewModel.SelectedAccountType = AccountType.Revenue;
        viewModel.AccountCode.ShouldBe("4100");

        viewModel.SelectedAccountType = AccountType.Expense;
        viewModel.AccountCode.ShouldBe("5100");

        viewModel.AccountName = "Admin Fees";
        viewModel.AccountCode.ShouldBe("6100");
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
            new() { Id = "1", Code = "1000", Type = AccountType.Asset, OrganisationId = "org-1", Name = "A" },
            new() { Id = "2", Code = "1005", Type = AccountType.Asset, OrganisationId = "org-1", Name = "B" }
        };
        _accountFacade.GetChartOfAccounts(Arg.Any<string>()).Returns(orgId => Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = accounts }));

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 2, "existing accounts to load before suggesting a code");

        viewModel.SelectedAccountType = AccountType.Asset;

        viewModel.AccountCode.ShouldBe("1105");
    }

    [Fact]
    public async Task SuggestAccountCode_ShouldUseSiblingGapStrategy_WhenParentIsSelected()
    {
        var parentId = "parent-1";
        var accounts = new List<AccountData>
        {
            new() { Id = parentId, Code = "1100", Type = AccountType.Asset, OrganisationId = "org-1", Name = "Assets" },
            new() { Id = "1", Code = "1110", Type = AccountType.Asset, OrganisationId = "org-1", Name = "A", ParentId = parentId },
            new() { Id = "2", Code = "1115", Type = AccountType.Asset, OrganisationId = "org-1", Name = "B", ParentId = parentId }
        };
        _accountFacade.GetChartOfAccounts(Arg.Any<string>()).Returns(orgId =>
            Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = accounts }));

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1, "existing account tree to load before suggesting sibling code");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.SelectedParentAccount = viewModel.Accounts.Single();

        viewModel.AccountCode.ShouldBe("1125");
    }

    [Fact]
    public async Task SuggestAccountCode_ShouldRecomputeWhenParentChanges()
    {
        var parent1Id = "parent-1";
        var parent2Id = "parent-2";
        var accounts = new List<AccountData>
        {
            new() { Id = parent1Id, Code = "1100", Type = AccountType.Asset, OrganisationId = "org-1", Name = "Assets 1" },
            new() { Id = parent2Id, Code = "1200", Type = AccountType.Asset, OrganisationId = "org-1", Name = "Assets 2" },
            new() { Id = "child-1", Code = "1110", Type = AccountType.Asset, OrganisationId = "org-1", Name = "A", ParentId = parent1Id },
            new() { Id = "child-2", Code = "1115", Type = AccountType.Asset, OrganisationId = "org-1", Name = "B", ParentId = parent1Id },
            new() { Id = "child-3", Code = "1210", Type = AccountType.Asset, OrganisationId = "org-1", Name = "C", ParentId = parent2Id }
        };
        _accountFacade.GetChartOfAccounts(Arg.Any<string>()).Returns(orgId =>
            Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = accounts }));

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 2, "existing account tree to load before suggesting by parent");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.SelectedParentAccount = viewModel.Accounts.Single(a => a.Id == parent1Id);
        viewModel.AccountCode.ShouldBe("1125");

        viewModel.SelectedParentAccount = viewModel.Accounts.Single(a => a.Id == parent2Id);
        viewModel.AccountCode.ShouldBe("1220");
    }

    [Fact]
    public async Task SuggestAccountCode_WhenParentChangesToCollidingSuggestion_ShouldShowValidationError()
    {
        var parentId = "parent-1";
        var accounts = new List<AccountData>
        {
            new() { Id = "top-1", Code = "1110", Type = AccountType.Asset, OrganisationId = "org-1", Name = "Main Asset" },
            new() { Id = parentId, Code = "1100", Type = AccountType.Asset, OrganisationId = "org-1", Name = "Assets Group" }
        };
        _accountFacade.GetChartOfAccounts(Arg.Any<string>()).Returns(orgId =>
            Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = accounts }));

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count == 2, "existing accounts to load before parent collision check");

        viewModel.SelectedAccountType = AccountType.Asset;
        viewModel.SelectedParentAccount = viewModel.Accounts.Single(a => a.Id == parentId);

        viewModel.AccountCode.ShouldBe("1110");
        viewModel.HasErrors.ShouldBeTrue();
        var errorMessages = viewModel.GetErrors(nameof(viewModel.AccountCode))
            .Select(error => error.ToString())
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();
        errorMessages.ShouldContain(message => message!.Contains("Account code already exists.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SuggestAccountCode_ShouldThrow_WhenBlockIsExhausted()
    {
        var accounts = Enumerable.Range(1000, 1000)
            .Select(index => new AccountData
            {
                Id = $"asset-{index}",
                Code = index.ToString(),
                Type = AccountType.Asset,
                OrganisationId = "org-1",
                Name = $"Asset {index}"
            })
            .ToList();

        _accountFacade.GetChartOfAccounts(Arg.Any<string>()).Returns(orgId =>
            Task.FromResult(new ChartOfAccountsData { OrganisationId = orgId.ArgAt<string>(0), Accounts = accounts }));

        var viewModel = CreateViewModel();
        await InvokePrivateLoadAccountsAsync(viewModel);
        await WaitUntilAsync(() => viewModel.Accounts.Count >= 1, "exhausted block accounts to load");

        var exception = Should.Throw<InvalidOperationException>(() => viewModel.SelectedAccountType = AccountType.Asset);
        exception.Message.ShouldContain("[1000-1999]");
        exception.Message.ShouldContain("All 1000 numbers are already used");
    }

    [Fact]
    public async Task SaveOpeningBalance_ShouldUpdateParentComputedBalance()
    {
        var parent = new AccountData
        {
            Id = "parent-1",
            OrganisationId = "org-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };
        var leaf = new AccountData
        {
            Id = "leaf-1",
            OrganisationId = "org-1",
            Code = "1010",
            Name = "Cash",
            ParentId = "parent-1",
            Type = AccountType.Asset
        };

        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(Task.FromResult(new ChartOfAccountsData
            {
                OrganisationId = "org-1",
                Accounts = [parent, leaf]
            }));

        var openingBalanceCallCount = 0;
        _accountFacade.GetAccountOpeningBalance("org-1", "leaf-1", "fy-2025", "p-2025-01")
            .Returns(_ =>
            {
                openingBalanceCallCount++;
                return Task.FromResult(openingBalanceCallCount >= 3 ? 123.45m : 0m);
            });

        var viewModel = CreateViewModel();
        await WaitUntilAsync(() => viewModel.Accounts.Count == 1 && viewModel.Accounts[0].SubAccounts.Count == 1,
            "account hierarchy to load");

        var leafItem = viewModel.Accounts[0].SubAccounts[0];
        leafItem.OpeningBalance = 123.45m;

        await leafItem.SaveOpeningBalanceCommand!.ExecuteAsync(null);

        await _accountFacade.Received(1)
            .SetAccountOpeningBalance("org-1", "leaf-1", "fy-2025", "p-2025-01", 123.45m);
        viewModel.Accounts[0].OpeningBalance.ShouldBe(123.45m);
        leafItem.IsOpeningBalanceLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveOpeningBalance_ShouldUpdateRootComputedBalance()
    {
        var root = new AccountData
        {
            Id = "root-1",
            OrganisationId = "org-1",
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset
        };
        var parent = new AccountData
        {
            Id = "parent-1",
            OrganisationId = "org-1",
            Code = "1100",
            Name = "Current Assets",
            ParentId = "root-1",
            Type = AccountType.Asset
        };
        var leaf = new AccountData
        {
            Id = "leaf-1",
            OrganisationId = "org-1",
            Code = "1110",
            Name = "Cash",
            ParentId = "parent-1",
            Type = AccountType.Asset
        };

        _accountFacade.GetChartOfAccounts("org-1")
            .Returns(Task.FromResult(new ChartOfAccountsData
            {
                OrganisationId = "org-1",
                Accounts = [root, parent, leaf]
            }));

        var openingBalanceCallCount = 0;
        _accountFacade.GetAccountOpeningBalance("org-1", "leaf-1", "fy-2025", "p-2025-01")
            .Returns(_ =>
            {
                openingBalanceCallCount++;
                return Task.FromResult(openingBalanceCallCount >= 3 ? 88.5m : 0m);
            });

        var viewModel = CreateViewModel();
        await WaitUntilAsync(() =>
                viewModel.Accounts.Count == 1 &&
                viewModel.Accounts[0].SubAccounts.Count == 1 &&
                viewModel.Accounts[0].SubAccounts[0].SubAccounts.Count == 1,
            "three-level account hierarchy to load");

        var rootItem = viewModel.Accounts[0];
        var parentItem = rootItem.SubAccounts[0];
        var leafItem = parentItem.SubAccounts[0];
        leafItem.OpeningBalance = 88.5m;

        await leafItem.SaveOpeningBalanceCommand!.ExecuteAsync(null);

        await _accountFacade.Received(1)
            .SetAccountOpeningBalance("org-1", "leaf-1", "fy-2025", "p-2025-01", 88.5m);
        parentItem.OpeningBalance.ShouldBe(88.5m);
        rootItem.OpeningBalance.ShouldBe(88.5m);
    }

    private static async Task InvokePrivateLoadAccountsAsync(ChartOfAccountsViewModel viewModel)
    {
        var loadMethod = typeof(ChartOfAccountsViewModel).GetMethod(
            "LoadAccounts",
            BindingFlags.NonPublic | BindingFlags.Instance);

        loadMethod.ShouldNotBeNull();
        loadMethod.Invoke(viewModel, null);
        await Task.Yield();
    }

    private static async Task InvokePrivateToggleAccountActiveAsync(ChartOfAccountsViewModel viewModel,
        AccountItemViewModel item)
    {
        var toggleMethod = typeof(ChartOfAccountsViewModel).GetMethod(
            "ToggleAccountActiveAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        toggleMethod.ShouldNotBeNull();

        var result = toggleMethod.Invoke(viewModel, [item]);
        if (result is not Task toggleTask)
            throw new InvalidOperationException("ToggleAccountActiveAsync did not return a Task.");

        await toggleTask;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string expectation, int timeoutMilliseconds = 1500)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;

            await Task.Delay(25);
        }

        condition().ShouldBeTrue($"Timed out waiting for {expectation}.");
    }
}
