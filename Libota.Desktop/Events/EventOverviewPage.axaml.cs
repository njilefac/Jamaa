using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Events;

public partial class EventOverviewPage : UserControl
{
    public EventOverviewPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}