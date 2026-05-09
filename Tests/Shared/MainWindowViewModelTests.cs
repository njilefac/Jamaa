using System;
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
        // Arrange
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.AccountingTransactions));

        // Assert
        viewModel.SelectedItem.ShouldNotBeNull();
        viewModel.SelectedItem!.Title.ShouldBe("Accounting");
    }

    [Fact]
    public void Receive_ShouldSelectBuiltInSettingsItemForNestedConfigurationRoutes()
    {
        // Arrange
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.FiscalCalendarAndPeriods));

        // Assert
        viewModel.SelectedItem.ShouldBeNull();
    }

    [Fact]
    public void Receive_ShouldSelectBuiltInSettingsItemForSettingsRoute()
    {
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

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
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        viewModel.NavigateToSettings();

        viewModel.ActiveModule.ShouldBe(settingsHost);
    }

    [Fact]
    public void Receive_ShouldUseSettingsHostForAccountingConfigurationSubRoutes()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var settingsHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.Settings, Arg.Any<object?>()).Returns(settingsHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.ChartOfAccounts));

        // Assert
        viewModel.ActiveModule.ShouldBe(settingsHost);
        ((INavigationHost)settingsHost).Received(1).NavigateTo(Routes.ChartOfAccounts);
        routeResolver.Received(1).Resolve(Routes.Settings);
    }

    [Fact]
    public void Receive_ShouldNotDelegateToNavigationHostWhenRouteIsModuleRoot()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var membersHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.MembersOverview, Arg.Any<object?>()).Returns(membersHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.MembersOverview));

        // Assert
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
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        var parameter = new object();

        viewModel.Receive(new ModuleSelected(Routes.AccountingTransactions, parameter));

        ((INavigationHost)accountingHost).Received(1).NavigateTo(Routes.AccountingTransactions, parameter);
    }

    private static IRouteResolver CreateRouteResolver()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));

        return routeResolver;
    }

    private sealed class TestApplicationModule(string route) : IApplicationModule
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; } = route;
        public object? HeaderContent => null;
    }
}