using Avalonia.Controls;
using Avalonia.Input;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Members.ViewModels;

namespace Jamaa.Desktop.Members.Components;

public partial class MembersGrid : UserControl
{
    public MembersGrid()
    {
        InitializeComponent();
    }

    private void OnMembersDataGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ShowSelectedMemberProfile();
        e.Handled = true;
    }

    private void OnMembersDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        ShowSelectedMemberProfile();
        e.Handled = true;
    }

    private void ShowSelectedMemberProfile()
    {
        if (DataContext is not MemberListViewModel vm || MembersDataGrid.SelectedItem is not MemberViewModel member)
            return;

        var args = new MemberProfileNavigationArgs(MemberListViewModel.MapToData(member), "General");
        if (vm.ShowMemberProfileCommand.CanExecute(args))
            vm.ShowMemberProfileCommand.Execute(args);
    }
}
