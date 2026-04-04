using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Jamaa.Desktop.Dashboard;

public partial class MembershipStatsWidgetViewModel : WidgetViewModelBase
{
    [property: JsonIgnore] [ObservableProperty] private int _totalMembers;
    [property: JsonIgnore] [ObservableProperty] private int _newMembersThisMonth;
    [property: JsonIgnore] [ObservableProperty] private int _activeMembers;

    public MembershipStatsWidgetViewModel()
    {
        Title = "Membership Stats";
        AllowedBoxSize = BoxSize.Small;
        IsRemovable = true;

        // Mock data
        TotalMembers = 150;
        NewMembersThisMonth = 12;
        ActiveMembers = 138;
    }
}
