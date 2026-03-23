using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Setup;

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