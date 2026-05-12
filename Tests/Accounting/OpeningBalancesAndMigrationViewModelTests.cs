using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

public class OpeningBalancesAndMigrationViewModelTests
{
    private const string OrgId = "org-1";
    private readonly IAccountingFacade _accountingFacade;
    private readonly IUserSessionService _userSessionService;
    private readonly INotificationService _notificationService;
    private readonly OpeningBalancesAndMigrationViewModel _viewModel;

    public OpeningBalancesAndMigrationViewModelTests()
    {
        _accountingFacade = Substitute.For<IAccountingFacade>();
        _userSessionService = Substitute.For<IUserSessionService>();
        _notificationService = Substitute.For<INotificationService>();

        var organisation = new OrganisationData { Id = OrgId, Name = "Test Org" };
        var session = new UserSession(true, "admin", Guid.NewGuid(), organisation);
        _userSessionService.CurrentUserSession.Returns(session);

        _accountingFacade.GetAccountingSettings(OrgId).Returns(Task.FromResult(new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = "USD",
            DateFormat = "yyyy-MM-dd",
            DecimalPrecision = 2,
            AvailableCurrencies = new List<AccountingAvailableCurrencyData>
            {
                new() { OrganisationId = OrgId, CurrencyCode = "USD", CurrencySymbol = "$" }
            }
        }));

        _accountingFacade.GetChartOfAccounts(OrgId).Returns(Task.FromResult(new ChartOfAccountsData
        {
            OrganisationId = OrgId,
            Accounts = new List<AccountData>
            {
                new() { Id = "acc-1", OrganisationId = OrgId, Code = "1000", Name = "Assets", Type = Domain.Accounting.Values.AccountType.Asset },
                new() { Id = "acc-1-1", OrganisationId = OrgId, Code = "1100", Name = "Cash", ParentId = "acc-1", Type = Domain.Accounting.Values.AccountType.Asset }
            }
        }));

        // Setup AccountOpeningBalanceSet observable to avoid null reference if subscribed
        _accountingFacade.AccountOpeningBalanceSet.Returns(Observable.Never<AccountingPeriodBalanceData>());
        _accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData { OrganisationId = OrgId, FiscalYears = new List<FiscalYearData>() }));

        _viewModel = new OpeningBalancesAndMigrationViewModel(_accountingFacade, _userSessionService, _notificationService);
    }

    [Fact]
    public async Task LoadAccountsAsync_SelectsFirstOpenPeriodOfFirstOpenFiscalYear()
    {
        // Arrange
        var fy2024 = new FiscalYearData
        {
            Id = "fy-2024",
            OrganisationId = OrgId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            IsLocked = true,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2024-01", FiscalYearId = "fy-2024", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = true }
            }
        };

        var fy2025 = new FiscalYearData
        {
            Id = "fy-2025",
            OrganisationId = OrgId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsLocked = false,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2025-01", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = true },
                new() { Id = "p-2025-02", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 2, IsLocked = false },
                new() { Id = "p-2025-03", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 3, IsLocked = false }
            }
        };

        var fy2026 = new FiscalYearData
        {
            Id = "fy-2026",
            OrganisationId = OrgId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            IsLocked = false,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2026-01", FiscalYearId = "fy-2026", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = false }
            }
        };

        // Note: FiscalCalendarQueryHandler returns them descending by StartDate, simulating that.
        var fiscalCalendar = new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears = new List<FiscalYearData> { fy2026, fy2025, fy2024 }
        };

        _accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(fiscalCalendar));

        // Act
        await _viewModel.LoadAccountsAsync();

        // Assert
        var leafAccount = _viewModel.LeafAccounts.FirstOrDefault(a => a.Id == "acc-1-1");
        leafAccount.ShouldNotBeNull();
        leafAccount.FiscalYearId.ShouldBe("fy-2025");
        leafAccount.AccountingPeriodId.ShouldBe("p-2025-02");
    }

    [Fact]
    public async Task SaveOpeningBalance_SendsRequestWithCorrectPeriod()
    {
        // Arrange
        var fy2025 = new FiscalYearData
        {
            Id = "fy-2025",
            OrganisationId = OrgId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsLocked = false,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2025-01", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = false }
            }
        };
        _accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData { OrganisationId = OrgId, FiscalYears = new List<FiscalYearData> { fy2025 } }));
        
        await _viewModel.LoadAccountsAsync();

        var leafAccount = _viewModel.LeafAccounts.First(a => a.Id == "acc-1-1");
        leafAccount.OpeningBalance = 123.45m;

        // Act
        await leafAccount.SaveOpeningBalanceCommand.ExecuteAsync(null);

        // Assert
        await _accountingFacade.Received(1).SetAccountOpeningBalance(
            OrgId,
            "acc-1-1",
            "fy-2025",
            "p-2025-01",
            123.45m
        );
    }

    [Fact]
    public async Task SaveOpeningBalance_ShowsNotificationOnSuccess()
    {
        // Arrange
        var fy2025 = new FiscalYearData
        {
            Id = "fy-2025",
            OrganisationId = OrgId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsLocked = false,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2025-01", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = false }
            }
        };
        _accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData { OrganisationId = OrgId, FiscalYears = new List<FiscalYearData> { fy2025 } }));
        
        var balanceSubject = new System.Reactive.Subjects.Subject<AccountingPeriodBalanceData>();
        _accountingFacade.AccountOpeningBalanceSet.Returns(balanceSubject);

        await _viewModel.LoadAccountsAsync();

        var leafAccount = _viewModel.LeafAccounts.First(a => a.Id == "acc-1-1");
        leafAccount.OpeningBalance = 123.45m;

        // Act
        var saveTask = leafAccount.SaveOpeningBalanceCommand.ExecuteAsync(null);
        
        // Simulate event confirmation
        balanceSubject.OnNext(new AccountingPeriodBalanceData 
        { 
            AccountId = "acc-1-1", 
            FiscalYearId = "fy-2025", 
            AccountingPeriodId = "p-2025-01",
            OrganisationId = OrgId,
            OpeningBalance = 123.45m,
            Id = "acc-1-1-fy-2025-p-2025-01"
        });

        await saveTask;

        // Assert
        _notificationService.Received(1).Show(
            "Opening Balance",
            Arg.Is<string>(s => s.Contains("Saved") && s.Contains("1100 - Cash")),
            NotificationType.Success
        );
    }
    [Fact]
    public async Task SaveOpeningBalance_ShowsNotificationOnReadModelFallback()
    {
        // Arrange
        var fy2025 = new FiscalYearData
        {
            Id = "fy-2025",
            OrganisationId = OrgId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsLocked = false,
            Periods = new List<AccountingPeriodData>
            {
                new() { Id = "p-2025-01", FiscalYearId = "fy-2025", OrganisationId = OrgId, SequenceNumber = 1, IsLocked = false }
            }
        };
        _accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(new FiscalCalendarData { OrganisationId = OrgId, FiscalYears = new List<FiscalYearData> { fy2025 } }));
        
        // Never emit event
        _accountingFacade.AccountOpeningBalanceSet.Returns(Observable.Never<AccountingPeriodBalanceData>());

        // Mock read model to return the value after a short delay (simulated by multiple calls)
        var callCount = 0;
        _accountingFacade.GetAccountOpeningBalance(OrgId, "acc-1-1", "fy-2025", "p-2025-01")
            .Returns(_ => 
            {
                callCount++;
                return Task.FromResult(callCount >= 2 ? 123.45m : 0m);
            });

        await _viewModel.LoadAccountsAsync();

        var leafAccount = _viewModel.LeafAccounts.First(a => a.Id == "acc-1-1");
        leafAccount.OpeningBalance = 123.45m;

        // Act
        // We use a shorter timeout in the test if we could, but OpeningBalanceItemViewModel has it hardcoded to 10s.
        // The polling is every 250ms.
        await leafAccount.SaveOpeningBalanceCommand.ExecuteAsync(null);

        // Assert
        _notificationService.Received(1).Show(
            "Opening Balance",
            Arg.Is<string>(s => s.Contains("Saved") && s.Contains("1100 - Cash")),
            NotificationType.Success
        );
    }
}
