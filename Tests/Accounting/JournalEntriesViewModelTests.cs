using System.Linq;
using Jamaa.Desktop.Accounting;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class JournalEntriesViewModelTests
{
    [Fact]
    public void Constructor_ShouldDefaultToAllAccountsFilter()
    {
        var viewModel = new JournalEntriesViewModel();

        viewModel.ExpenseAccountsOnly.ShouldBeFalse();
        viewModel.ActiveFilterLabel.ShouldBe("All Accounts");
        viewModel.VisibleEntries.Any(entry => entry.IsExpenseAccount).ShouldBeTrue();
        viewModel.VisibleEntries.Any(entry => !entry.IsExpenseAccount).ShouldBeTrue();
    }

    [Fact]
    public void ApplyNavigationFilter_ShouldShowOnlyExpenseAccounts_WhenExpenseFilterRequested()
    {
        var viewModel = new JournalEntriesViewModel();

        viewModel.ApplyNavigationFilter(JournalEntriesNavigationRequest.OnlyExpenseAccounts());

        viewModel.ExpenseAccountsOnly.ShouldBeTrue();
        viewModel.ActiveFilterLabel.ShouldBe("Expense Accounts");
        viewModel.VisibleEntries.ShouldNotBeEmpty();
        viewModel.VisibleEntries.All(entry => entry.IsExpenseAccount).ShouldBeTrue();
    }

    [Fact]
    public void ShowAllEntriesCommand_ShouldClearExpenseFilter_WhenPreviouslyFiltered()
    {
        var viewModel = new JournalEntriesViewModel();
        viewModel.ApplyNavigationFilter(JournalEntriesNavigationRequest.OnlyExpenseAccounts());

        viewModel.ShowAllEntriesCommand.Execute(null);

        viewModel.ExpenseAccountsOnly.ShouldBeFalse();
        viewModel.ActiveFilterLabel.ShouldBe("All Accounts");
        viewModel.VisibleEntries.Any(entry => entry.IsExpenseAccount).ShouldBeTrue();
        viewModel.VisibleEntries.Any(entry => !entry.IsExpenseAccount).ShouldBeTrue();
    }
}



