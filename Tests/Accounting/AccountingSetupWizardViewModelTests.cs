using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting.Wizard;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class AccountingSetupWizardViewModelTests
{
    private const string OrgId = "org-1";

    [Fact]
    public void Next_WhenAlreadyOnLastStep_DoesNotThrow()
    {
        var viewModel = CreateViewModel();
        viewModel.CurrentStepIndex = viewModel.Steps.Count - 1;

        var nextMethod = typeof(AccountingSetupWizardViewModel)
            .GetMethod("Next", BindingFlags.Instance | BindingFlags.NonPublic);

        nextMethod.ShouldNotBeNull();

        Should.NotThrow(() => nextMethod!.Invoke(viewModel, null));
        viewModel.CurrentStepIndex.ShouldBe(viewModel.Steps.Count - 1);
    }

    [Fact]
    public async Task RefreshStepStateAsync_WhenFirstStepIsComplete_SelectsTheChartStep()
    {
        var settings = new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = "USD",
            DateFormat = "yyyy-MM-dd",
            DecimalPrecision = 2,
            ThousandSeparator = ",",
            AvailableCurrencies =
            [
                new AccountingAvailableCurrencyData
                {
                    OrganisationId = OrgId,
                    CurrencyCode = "USD",
                    CurrencySymbol = "$"
                }
            ]
        };

        var fiscalCalendar = new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears =
            [
                new FiscalYearData
                {
                    Id = "fy-1",
                    OrganisationId = OrgId,
                    StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsLocked = false
                }
            ]
        };

        var viewModel = CreateViewModel(settings, fiscalCalendar, new ChartOfAccountsData { OrganisationId = OrgId }, false);

        await RefreshStepStateAsync(viewModel);

        viewModel.Steps[0].IsCompleted.ShouldBeTrue();
        viewModel.Steps[1].IsCompleted.ShouldBeFalse();
        viewModel.Steps[1].IsEnabled.ShouldBeTrue();
        viewModel.Steps[2].IsEnabled.ShouldBeFalse();
        viewModel.CurrentStepIndex.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshStepStateAsync_WhenSetupIsComplete_SelectsTheFinalStep()
    {
        var settings = new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = "USD",
            DateFormat = "yyyy-MM-dd",
            DecimalPrecision = 2,
            ThousandSeparator = ",",
            AvailableCurrencies =
            [
                new AccountingAvailableCurrencyData
                {
                    OrganisationId = OrgId,
                    CurrencyCode = "USD",
                    CurrencySymbol = "$"
                }
            ]
        };

        var fiscalCalendar = new FiscalCalendarData
        {
            OrganisationId = OrgId,
            FiscalYears =
            [
                new FiscalYearData
                {
                    Id = "fy-1",
                    OrganisationId = OrgId,
                    StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 12, 31),
                    IsLocked = false
                }
            ]
        };

        var chartOfAccounts = new ChartOfAccountsData
        {
            OrganisationId = OrgId,
            Accounts =
            [
                new AccountData
                {
                    Id = "acc-1",
                    OrganisationId = OrgId,
                    Code = "1000",
                    Name = "Cash",
                    Type = Domain.Accounting.Values.AccountType.Asset
                }
            ]
        };

        var viewModel = CreateViewModel(settings, fiscalCalendar, chartOfAccounts, true);

        await RefreshStepStateAsync(viewModel);

        foreach (var step in viewModel.Steps)
        {
            step.IsCompleted.ShouldBeTrue();
            step.IsEnabled.ShouldBeTrue();
        }
        viewModel.CurrentStepIndex.ShouldBe(2);
    }

    private static AccountingSetupWizardViewModel CreateViewModel(
        AccountingSettingsData? settings = null,
        FiscalCalendarData? fiscalCalendar = null,
        ChartOfAccountsData? chartOfAccounts = null,
        bool isSetupComplete = false)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(Arg.Any<Type>()).Returns(new object());

        var accountingFacade = Substitute.For<IAccountingFacade>();
        accountingFacade.GetAccountingSettings(OrgId).Returns(Task.FromResult(settings));
        accountingFacade.GetFiscalCalendar(OrgId).Returns(Task.FromResult(fiscalCalendar ?? new FiscalCalendarData { OrganisationId = OrgId }));
        accountingFacade.GetChartOfAccounts(OrgId).Returns(Task.FromResult(chartOfAccounts ?? new ChartOfAccountsData { OrganisationId = OrgId }));
        accountingFacade.IsAccountingSetupComplete(OrgId).Returns(Task.FromResult(isSetupComplete));
        accountingFacade.CurrentAccountingSettings.Returns(Observable.Never<AccountingSettingsData?>());
        accountingFacade.AccountingSettingsUpdated.Returns(Observable.Never<AccountingSettingsData>());
        accountingFacade.CurrentFiscalCalendar.Returns(Observable.Never<FiscalCalendarData>());
        accountingFacade.FiscalCalendarUpdated.Returns(Observable.Never<FiscalCalendarData>());
        accountingFacade.AccountCreated.Returns(Observable.Never<AccountData>());
        accountingFacade.AccountUpdated.Returns(Observable.Never<AccountData>());
        accountingFacade.AccountDeleted.Returns(Observable.Never<AccountData>());
        accountingFacade.AccountDeactivated.Returns(Observable.Never<AccountData>());
        accountingFacade.AccountReactivated.Returns(Observable.Never<AccountData>());
        accountingFacade.AccountOpeningBalanceSet.Returns(Observable.Never<AccountingPeriodBalanceData>());

        var organisation = new OrganisationData { Id = OrgId, Name = "Test Org" };
        var session = new UserSession(true, "admin", Guid.NewGuid(), organisation);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(session);

        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver.Resolve(Arg.Any<string>(), Arg.Any<object?>()).Returns(new object());

        return new AccountingSetupWizardViewModel(serviceProvider, accountingFacade, userSessionService, routeResolver);
    }

    private static async Task RefreshStepStateAsync(AccountingSetupWizardViewModel viewModel)
    {
        var method = typeof(AccountingSetupWizardViewModel)
            .GetMethod("RefreshStepStateAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.ShouldNotBeNull();

        var task = (Task)method!.Invoke(viewModel, null)!;
        await task;
    }
}
