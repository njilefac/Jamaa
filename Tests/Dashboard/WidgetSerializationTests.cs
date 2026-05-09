using System.Text.Json;
using Jamaa.Desktop.Dashboard;
using Shouldly;
using Xunit;

namespace UnitTests.Dashboard;

public class WidgetSerializationTests
{
    [Fact]
    public void WidgetViewModelBase_Properties_ShouldBeIgnored()
    {
        // Arrange
        var widget = new RecentActivityFeedWidgetViewModel
        {
            Row = 1,
            Column = 2,
            Title = "Should be ignored",
            Id = "FixedID"
        };

        // Act
        var json = JsonSerializer.Serialize(widget);

        // Assert
        json.ShouldNotContain("\"Title\"");
        json.ShouldNotContain("\"Id\"");
        json.ShouldNotContain("\"AllowedBoxSize\"");
        json.ShouldNotContain("\"IsRemovable\"");
        json.ShouldNotContain("\"IsDraggingOver\"");
        json.ShouldNotContain("\"IsValidDrop\"");
        json.ShouldNotContain("\"ParentViewModel\"");
        json.ShouldNotContain("\"RemoveCommand\"");
        json.ShouldNotContain("\"CompatibleWidgets\"");
        json.ShouldNotContain("\"HasCompatibleWidgets\"");
        json.ShouldNotContain("\"FlyoutTitle\"");

        json.ShouldContain("\"Row\":1");
        json.ShouldContain("\"Column\":2");
    }

    [Fact]
    public void MembershipStatsWidgetViewModel_Properties_ShouldBeIgnored()
    {
        // Arrange
        var widget = new MembershipStatsWidgetViewModel
        {
            TotalMembers = 999,
            NewMembersThisMonth = 888,
            ActiveMembers = 777
        };

        // Act
        var json = JsonSerializer.Serialize(widget);

        // Assert
        json.ShouldNotContain("\"TotalMembers\"");
        json.ShouldNotContain("\"NewMembersThisMonth\"");
        json.ShouldNotContain("\"ActiveMembers\"");
    }

    [Fact]
    public void QuickActionsWidgetViewModel_Commands_ShouldBeIgnored()
    {
        // Arrange
        var widget = new QuickActionsWidgetViewModel();

        // Act
        var json = JsonSerializer.Serialize(widget);

        // Assert
        json.ShouldNotContain("\"RegisterMemberCommand\"");
        json.ShouldNotContain("\"ReceiveMoneyCommand\"");
        json.ShouldNotContain("\"SpendMoneyCommand\"");
    }
}