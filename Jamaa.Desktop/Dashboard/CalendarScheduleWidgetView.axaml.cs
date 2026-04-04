using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Dashboard;

public partial class CalendarScheduleWidgetView : UserControl
{
    public CalendarScheduleWidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}