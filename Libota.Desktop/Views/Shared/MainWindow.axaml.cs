using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
[UsedImplicitly]
public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
{

    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.FullScreen;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

#if DEBUG
        //this.AttachDevTools();
#endif
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Screens.Primary == null) return;
        var screenSize = Screens.Primary.WorkingArea.Size;
        var windowSize = PixelSize.FromSize(ClientSize, Screens.Primary.Scaling);

        Position = new PixelPoint(
            screenSize.Width - windowSize.Width,
            screenSize.Height - windowSize.Height);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}