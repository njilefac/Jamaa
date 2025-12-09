using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Groups;

namespace Libota.Desktop.Views.Groups;

[SingleInstanceView]
public partial class GroupManagementScreen : UserControl
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