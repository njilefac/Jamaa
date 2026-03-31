using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Jamaa.Desktop.Members.ViewModels;

namespace Jamaa.Desktop.Members.Components;

public partial class MembersGrid : UserControl
{
    public MembersGrid()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Enter)
        {
            if (DataContext is MemberListViewModel vm && vm.Selection.SelectedItem is MemberViewModel selectedMember)
            {
                vm.ShowMemberProfileCommand.Execute(selectedMember);
                e.Handled = true;
            }
        }
    }
}
