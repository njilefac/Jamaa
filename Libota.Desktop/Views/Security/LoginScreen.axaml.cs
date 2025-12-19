using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Views.Security;

public partial class LoginScreen : UserControl
{
    public LoginScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var userNameField = this.FindControl<TextBox>("UserNameField");
        userNameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }
}