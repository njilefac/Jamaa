using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Dashboard;

public partial class RecentActivityFeedWidgetView : UserControl
{
    public RecentActivityFeedWidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}