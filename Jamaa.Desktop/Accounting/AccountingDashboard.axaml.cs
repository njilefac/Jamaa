using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingDashboard : UserControl
{
    public AccountingDashboard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}