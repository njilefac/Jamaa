using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.ViewModels.Members;

public partial class MemberManagementScreenViewModel : ObservableObject
{
    [ObservableProperty] private MemberListViewModel _memberListViewModel;
    [ObservableProperty] private MemberProfileViewModel _memberProfileViewModel;

    public MemberManagementScreenViewModel(MembersOverviewPageViewModel membersOverviewPageViewModel,
        MemberListViewModel memberListViewModel, MemberProfileViewModel memberProfileViewModel)
    {
        _memberListViewModel = memberListViewModel;
        _memberProfileViewModel = memberProfileViewModel;
        CurrentContent = membersOverviewPageViewModel;
    }

    [ObservableProperty] private ObservableObject? _currentContent;
}