using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.ViewModels.Setup;

namespace Libota.Desktop.Views.Setup;

public partial class OrganisationContactDetailsScreen : UserControl
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
}