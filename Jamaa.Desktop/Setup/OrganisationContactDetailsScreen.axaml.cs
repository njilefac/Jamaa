using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Setup;

public partial class OrganisationContactDetailsScreen : UserControl
{
    public OrganisationContactDetailsScreen()
    {
        InitializeComponent();
    }

    public OrganisationContactDetailsScreen(OrganisationContactDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}