using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Dashboard;

public partial class BookkeepingWidgetView : UserControl
{
    public BookkeepingWidgetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
