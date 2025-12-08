using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Setup;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class CreateOrganisationScreen : UserControl, IViewFor<CreateOrganisationViewModel>
{
    public CreateOrganisationScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var nameField = this.FindControl<TextBox>("OrganisationNameField");
        nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }

    public new CreateOrganisationViewModel? DataContext { 
        get => base.DataContext as CreateOrganisationViewModel;
        set => base.DataContext = value; 
    }
}