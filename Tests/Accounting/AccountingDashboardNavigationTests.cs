using System.Linq;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
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
    public void CreateActionShortcuts_ShouldNotIncludeAccountingConfigurationShortcut()
    {
        // Arrange
        var routeResolver = Substitute.For<IRouteResolver>();
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        // Act
        var shortcuts = viewModel.ActionShortcuts;

        // Assert
        shortcuts.Any(s => s.Title == "Accounting Configuration").ShouldBeFalse();
    }

    [Fact]
    public void CreateActionShortcuts_ShouldIncludeTotalExpensesShortcut()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        var totalExpensesShortcut = viewModel.ActionShortcuts.Single(s => s.Title == "Total Expenses");

        totalExpensesShortcut.ShouldNotBeNull();
        totalExpensesShortcut.Action.ShouldNotBeNull();
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
    public void TotalExpensesShortcut_ShouldNavigateToJournalEntriesWithExpenseFilter()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        var journalEntriesViewModel = new JournalEntriesViewModel();
        routeResolver.Resolve(Routes.AccountingTransactions, Arg.Any<object?>()).Returns(journalEntriesViewModel);
        var viewModel = new AccountingDashboardViewModel(routeResolver);

        var totalExpensesShortcut = viewModel.ActionShortcuts.Single(s => s.Title == "Total Expenses");
        totalExpensesShortcut.Action.Execute(null);

        viewModel.SelectedTabIndex.ShouldBe(1);
        viewModel.Breadcrumbs.Count.ShouldBe(2);
        viewModel.Breadcrumbs[0].Title.ShouldBe("Accounting");
        viewModel.Breadcrumbs[1].Title.ShouldBe("Journal Entries");
        journalEntriesViewModel.ExpenseAccountsOnly.ShouldBeTrue();
        journalEntriesViewModel.VisibleEntries.All(entry => entry.IsExpenseAccount).ShouldBeTrue();
    }

}



