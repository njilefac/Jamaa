using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Security;

[SingleInstanceView]
public partial class LoginScreen : UserControl, IViewFor<LoginScreenViewModel>
{
    public LoginScreen(LoginScreenViewModel loginScreenViewModel, DashboardViewModel dashboardViewModel)
    {
        InitializeComponent();
        DataContext = loginScreenViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var userNameField = this.FindControl<TextBox>("UserNameField");
        userNameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }

    public new LoginScreenViewModel? DataContext
    {
        get => base.DataContext as LoginScreenViewModel;
        set => base.DataContext = value;
    }
}