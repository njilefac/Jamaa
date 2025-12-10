using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Events;

[SingleInstanceView]
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