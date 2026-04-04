using Avalonia.Markup.Xaml;
using Avalonia.Controls;

namespace Jamaa.Desktop.Dashboard;

public partial class MembershipStatsWidgetView : UserControl
{
    public MembershipStatsWidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
