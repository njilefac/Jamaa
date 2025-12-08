using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Groups;

namespace Libota.Desktop.Views.Groups;

[SingleInstanceView]
public partial class GroupManagementScreen : UserControl, IViewFor<GroupManagementViewModel>
{
    public GroupManagementScreen(GroupManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new GroupManagementViewModel? DataContext { get; set; }
}