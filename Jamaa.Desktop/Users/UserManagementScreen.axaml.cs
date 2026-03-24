using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Users;

[UsedImplicitly]
public partial class UserManagementScreen : UserControl
{
    public UserManagementScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}