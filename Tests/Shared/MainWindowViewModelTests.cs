using System;
using System.Threading;
using System.Threading.Tasks;
using Jamaa.Application.Accounting;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Services;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Shared;

public class MainWindowViewModelTests
{
    [Fact]
    public void Receive_ShouldSelectAccountingItemForAccountingSubRoutes()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.Receive(new ModuleSelected(Routes.AccountingTransactions));

        viewModel.SelectedItem.ShouldNotBeNull();
        viewModel.SelectedItem!.Title.ShouldBe("Accounting");
    }

    [Fact]
    public void Receive_ShouldSelectBuiltInSettingsItemForNestedConfigurationRoutes()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.Receive(new ModuleSelected(Routes.FiscalCalendarAndPeriods));

        viewModel.SelectedItem.ShouldBeNull();
    }

    [Fact]
    public void Receive_ShouldSelectBuiltInSettingsItemForSettingsRoute()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.Receive(new ModuleSelected(Routes.Settings));

        viewModel.SelectedItem.ShouldBeNull();
    }

    [Fact]
    public void NavigateToSettings_ShouldSendSettingsRouteToShell()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var settingsHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.Settings, Arg.Any<object?>()).Returns(settingsHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.NavigateToSettings();

        viewModel.ActiveModule.ShouldBe(settingsHost);
    }

    [Fact]
    public void Receive_ShouldUseSettingsHostForAccountingConfigurationSubRoutes()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var settingsHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.Settings, Arg.Any<object?>()).Returns(settingsHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.Receive(new ModuleSelected(Routes.ChartOfAccounts));

        viewModel.ActiveModule.ShouldBe(settingsHost);
        ((INavigationHost)settingsHost).Received(1).NavigateTo(Routes.ChartOfAccounts);
        routeResolver.Received(1).Resolve(Routes.Settings);
    }

    [Fact]
    public void Receive_ShouldNotDelegateToNavigationHostWhenRouteIsModuleRoot()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var membersHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.MembersOverview, Arg.Any<object?>()).Returns(membersHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        viewModel.Receive(new ModuleSelected(Routes.MembersOverview));

        viewModel.ActiveModule.ShouldBe(membersHost);
        ((INavigationHost)membersHost).DidNotReceive().NavigateTo(Arg.Any<string>(), Arg.Any<object?>());
    }

    [Fact]
    public void Receive_ShouldForwardNavigationParameterToNavigationHost()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var accountingHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.AccountingDashboard, Arg.Any<object?>()).Returns(accountingHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        var parameter = new object();

        viewModel.Receive(new ModuleSelected(Routes.AccountingTransactions, parameter));

        ((INavigationHost)accountingHost).Received(1).NavigateTo(Routes.AccountingTransactions, parameter);
    }

    [Fact]
    public void Receive_ShouldShowAccountingSetupWizardWhenAccountingSetupIsIncomplete()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: false);
        var userSessionService = CreateUserSessionService();
        var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        try
        {
            viewModel.Receive(new ModuleSelected(Routes.AccountingDashboard));

            Thread.Sleep(50);
            viewModel.ActiveModule!.Title.ShouldBe(Routes.AccountingSetupWizard);
            viewModel.SelectedItem.ShouldNotBeNull();
            viewModel.SelectedItem!.Title.ShouldBe("Accounting");
        }
        finally
        {
            viewModel.Dispose();
        }
    }

    [Fact]
    public void Receive_ShouldShowAccountingDashboardWhenAccountingSetupIsComplete()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        var accountingFacade = CreateAccountingFacade(isSetupComplete: true);
        var userSessionService = CreateUserSessionService();
        var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider, accountingFacade,
            userSessionService);

        try
        {
            viewModel.Receive(new ModuleSelected(Routes.AccountingDashboard));

            Thread.Sleep(50);
            viewModel.ActiveModule!.Title.ShouldBe(Routes.AccountingDashboard);
            viewModel.SelectedItem.ShouldNotBeNull();
            viewModel.SelectedItem!.Title.ShouldBe("Accounting");
        }
        finally
        {
            viewModel.Dispose();
        }
    }

    private static IRouteResolver CreateRouteResolver()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));

        return routeResolver;
    }

    private static IAccountingFacade CreateAccountingFacade(bool isSetupComplete)
    {
        var accountingFacade = Substitute.For<IAccountingFacade>();
        accountingFacade.IsAccountingSetupComplete(Arg.Any<string>()).Returns(Task.FromResult(isSetupComplete));
        return accountingFacade;
    }

    private static IUserSessionService CreateUserSessionService()
    {
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(new UserSession(
            true,
            "Test User",
            Guid.NewGuid(),
            new OrganisationData
            {
                Id = "org-1",
                Name = "Test Organisation"
            }));
        return userSessionService;
    }

    private sealed class TestApplicationModule(string route) : IApplicationModule
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; } = route;
        public object? HeaderContent => null;
    }
}