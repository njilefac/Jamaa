using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Setup;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class OrganisationContactDetailsScreen : UserControl, IViewFor<OrganisationContactDetailsViewModel>
{
    public OrganisationContactDetailsScreen(OrganisationContactDetailsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public OrganisationContactDetailsViewModel? DataContext
    {
        get => base.DataContext as OrganisationContactDetailsViewModel;
        set => base.DataContext = value;
    }
}