using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class CreateSuperUserScreen : UserControl, IViewFor<CreateSuperUserViewModel>
{
    public CreateSuperUserScreen(CreateSuperUserViewModel createSuperUserViewModel, DashboardViewModel dashboardViewModel, LoginScreenViewModel loginScreenViewModel)
    {
        InitializeComponent();
        
        DataContext = createSuperUserViewModel;
        var dashBoardVm = dashboardViewModel;
        var loginVm = loginScreenViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var nameField = this.FindControl<TextBox>("UserNameField");
        nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }

    public new CreateSuperUserViewModel? DataContext
    {
        get => base.DataContext as CreateSuperUserViewModel;
        set => base.DataContext = value;
    }
}