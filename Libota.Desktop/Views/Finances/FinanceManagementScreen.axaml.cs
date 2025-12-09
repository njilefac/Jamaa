using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Finances;

namespace Libota.Desktop.Views.Finances;

[SingleInstanceView]
public partial class FinanceManagementScreen : UserControl
{
    public FinanceManagementScreen(FinanceManagementViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new FinanceManagementViewModel? DataContext { get; set; }
}