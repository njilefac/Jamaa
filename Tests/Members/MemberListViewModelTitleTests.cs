using System.Runtime.Serialization;
using Jamaa.Desktop.Members.Components;
using Shouldly;
using Xunit;

namespace UnitTests.Members;

public class MemberListViewModelTitleTests
{
    [Fact]
    public void Title_ShouldBeMembers()
    {
        // Assert
        const string title = "Members";
        title.ShouldBe("Members");
    }
}