using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
[UsedImplicitly]
public partial class MainWindow : Window
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}