using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Jamaa.Desktop.Dashboard;

public partial class MembershipStatsWidgetViewModel : WidgetViewModelBase
{
    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private int _newMembersThisMonth;
    [ObservableProperty] private int _activeMembers;

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
