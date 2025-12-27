using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Groups;

public partial class GroupManagementScreen : UserControl
{
    public GroupManagementScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}