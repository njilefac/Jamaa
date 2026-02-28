using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Finances;

public partial class FinanceOverviewPage : UserControl
{
    public FinanceOverviewPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}