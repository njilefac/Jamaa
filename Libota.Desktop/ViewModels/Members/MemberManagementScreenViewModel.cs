using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.ViewModels.Members;

public partial class MemberManagementScreenViewModel : ObservableObject
{
    [ObservableProperty] private MemberListViewModel _memberListViewModel;
    [ObservableProperty] private MemberProfileViewModel _memberProfileViewModel;

    public MemberManagementScreenViewModel(MemberOverviewPageViewModel memberOverviewPageViewModel,
        MemberListViewModel memberListViewModel, MemberProfileViewModel memberProfileViewModel)
    {
        _memberListViewModel = memberListViewModel;
        _memberProfileViewModel = memberProfileViewModel;
        CurrentContent = memberOverviewPageViewModel;
    }

    [ObservableProperty] private ObservableObject? _currentContent;
}