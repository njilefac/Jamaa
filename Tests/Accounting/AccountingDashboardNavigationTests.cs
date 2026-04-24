using System;
using System.Linq;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class AccountingDashboardNavigationTests
{
    [Fact]
    public void CreateActionShortcuts_ShouldNotIncludeConfigureChartShortcut()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        var shortcuts = viewModel.ActionShortcuts;

        // Assert
        shortcuts.Any(s => s.Title == "Configure Chart").ShouldBeFalse();
    }

    [Fact]
    public void CreateActionShortcuts_ShouldIncludeAccountingConfigurationShortcut()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        var shortcuts = viewModel.ActionShortcuts;

        // Assert
        var configurationShortcut = shortcuts.Single(s => s.Title == "Accounting Configuration");
        configurationShortcut.ShouldNotBeNull();
        configurationShortcut.Action.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSelectDashboardTabAndShowBreadcrumbHierarchy()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();

        // Act
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Assert
        viewModel.SelectedTabIndex.ShouldBe(0);
        viewModel.Breadcrumbs.Count.ShouldBe(2);
        viewModel.Breadcrumbs[0].Title.ShouldBe("Accounting");
        viewModel.Breadcrumbs[1].Title.ShouldBe("Dashboard");
    }

    [Fact]
    public void CreateMetricCards_ShouldIncludeChartAnomaliesCard()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        var metricCards = viewModel.MetricCards;

        // Assert
        var chartAnomaliesCard = metricCards.Single(c => c.MetricLabel == "Chart Anomalies");
        chartAnomaliesCard.ShouldNotBeNull();
        chartAnomaliesCard.MetricValue.ShouldBe("3");
        chartAnomaliesCard.NavigateCommand.ShouldNotBeNull();
    }

    [Fact]
    public void NavigateToChartOfAccounts_ShouldSelectConfigurationTabAndUpdateBreadcrumbs()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var configurationViewModel = new AccountingConfigurationViewModel(routeResolver);
        routeResolver.Resolve(Routes.AccountingConfiguration).Returns(configurationViewModel);
        routeResolver.Resolve(Routes.ChartOfAccounts).Returns(new TestRouteableModule("Chart of Accounts"));
        
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        viewModel.NavigateTo(Routes.ChartOfAccounts);
        
        // Assert
        routeResolver.Received(2).Resolve(Routes.ChartOfAccounts);
        viewModel.SelectedTabIndex.ShouldBe(4);
        viewModel.Breadcrumbs.Count.ShouldBe(3);
        viewModel.Breadcrumbs[0].Title.ShouldBe("Accounting");
        viewModel.Breadcrumbs[1].Title.ShouldBe("Configuration");
        viewModel.Breadcrumbs[2].Title.ShouldBe("Chart of Accounts");
    }

    [Fact]
    public void NavigateToAccountingDashboard_ShouldReturnToDashboardTab()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var configurationViewModel = new AccountingConfigurationViewModel(routeResolver);
        routeResolver.Resolve(Routes.AccountingConfiguration).Returns(configurationViewModel);
        routeResolver.Resolve(Routes.ChartOfAccounts).Returns(new TestRouteableModule("Chart of Accounts"));
        var viewModel = new AccountingDashboardViewModel(routeResolver);
        viewModel.NavigateTo(Routes.ChartOfAccounts);

        // Act
        viewModel.NavigateTo(Routes.AccountingDashboard);

        // Assert
        viewModel.SelectedTabIndex.ShouldBe(0);
        viewModel.Breadcrumbs.Count.ShouldBe(2);
        viewModel.Breadcrumbs[0].Title.ShouldBe("Accounting");
        viewModel.Breadcrumbs[1].Title.ShouldBe("Dashboard");
    }

    [Fact]
    public void NavigateToNestedConfigurationRoute_ShouldSelectConfigurationTabAndExtendBreadcrumbs()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var configurationViewModel = new AccountingConfigurationViewModel(routeResolver);
        routeResolver.Resolve(Routes.AccountingConfiguration).Returns(configurationViewModel);
        routeResolver.Resolve(Routes.FiscalCalendarAndPeriods).Returns(new TestRouteableModule("Fiscal Calendar & Periods"));
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        viewModel.NavigateTo(Routes.FiscalCalendarAndPeriods);

        // Assert
        viewModel.SelectedTabIndex.ShouldBe(4);
        viewModel.Breadcrumbs.Count.ShouldBe(3);
        viewModel.Breadcrumbs[0].Title.ShouldBe("Accounting");
        viewModel.Breadcrumbs[1].Title.ShouldBe("Configuration");
        viewModel.Breadcrumbs[2].Title.ShouldBe("Fiscal Calendar & Periods");
    }

    private sealed class TestRouteableModule(string title) : IRouteableViewModel, IApplicationModule
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; } = title;
        public object? HeaderContent => null;
    }
}



