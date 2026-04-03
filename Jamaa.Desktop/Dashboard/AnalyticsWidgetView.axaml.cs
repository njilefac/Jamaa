using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Dashboard;

public partial class AnalyticsWidgetView : UserControl
{
    public AnalyticsWidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
