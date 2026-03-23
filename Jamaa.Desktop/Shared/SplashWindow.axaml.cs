using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;

namespace Jamaa.Desktop.Shared;

public partial class SplashWindow : AppWindow
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
