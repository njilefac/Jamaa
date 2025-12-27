using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.Members.Components;

public partial class MembersSummary : ObservableObject
{
    [ObservableProperty]
    private uint _totalMembersCount;
    [ObservableProperty]
    private uint _maleMembersCount;
    [ObservableProperty]
    private uint _femaleMembersCount;
}