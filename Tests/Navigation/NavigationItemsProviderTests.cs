using System.Linq;
using Jamaa.Desktop.Services.Navigation.Services;
using Jamaa.Desktop.Services.Navigation.Values;
using Shouldly;
using Xunit;

namespace UnitTests.Navigation;

public class NavigationItemsProviderTests
{
    [Fact]
    public void GetNavigationItems_ShouldIncludeAccountingParentRoute()
    {
        // Arrange
        var provider = new NavigationItemsProvider();

        // Act
        var items = provider.GetNavigationItems().ToList();

        // Assert
        var accountingItem = items.Single(x => x.Title == "Accounting");
        accountingItem.TargetRoute.ShouldBe(Routes.AccountingDashboard);
    }

    [Fact]
    public void GetNavigationItems_ShouldExposeAccountingAsSingleLevelMenuItem()
    {
        // Arrange
        var provider = new NavigationItemsProvider();

        // Act
        var accountingItem = provider.GetNavigationItems().Single(x => x.Title == "Accounting");

        // Assert
        accountingItem.SubItems.ShouldBeNull();
    }

    [Fact]
    public void GetNavigationItems_ShouldNotIncludeSettingsRoute()
    {
        // Arrange
        var provider = new NavigationItemsProvider();

        // Act
        var items = provider.GetNavigationItems().ToList();

        // Assert
        items.Any(x => x.TargetRoute == Routes.Settings).ShouldBeFalse();
    }
}