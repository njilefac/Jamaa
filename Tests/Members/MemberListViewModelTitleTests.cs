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
        // Operation: validate the constant route title without invoking constructor side effects.
        var viewModel = (MemberListViewModel)FormatterServices.GetUninitializedObject(typeof(MemberListViewModel));

        // Assert
        viewModel.Title.ShouldBe("Members");
    }
}