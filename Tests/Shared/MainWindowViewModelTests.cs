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
    public void Receive_ShouldSelectAccountingItemForNestedConfigurationRoutes()
    {
        // Arrange
        var routeResolver = CreateRouteResolver();
        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.FiscalCalendarAndPeriods));

        // Assert
        viewModel.SelectedItem.ShouldNotBeNull();
        viewModel.SelectedItem!.Title.ShouldBe("Accounting");
    }

    [Fact]
    public void Receive_ShouldUseAccountingDashboardHostForAccountingSubRoutes()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var accountingHost = Substitute.For<IApplicationModule, INavigationHost>();
        routeResolver
            .Resolve(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(call => new TestApplicationModule((string)call[0]));
        routeResolver.Resolve(Routes.AccountingDashboard, Arg.Any<object?>()).Returns(accountingHost);

        var navigationItemsProvider = new NavigationItemsProvider();
        using var viewModel = new MainWindowViewModel(routeResolver, navigationItemsProvider);

        // Act
        viewModel.Receive(new ModuleSelected(Routes.ChartOfAccounts));

        // Assert
        viewModel.ActiveModule.ShouldBe(accountingHost);
        ((INavigationHost)accountingHost).Received(1).NavigateTo(Routes.ChartOfAccounts, null);
        routeResolver.Received(1).Resolve(Routes.AccountingDashboard, null);
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

