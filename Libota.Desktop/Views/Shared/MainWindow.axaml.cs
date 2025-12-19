using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;

namespace Libota.Desktop.Views.Shared;

[UsedImplicitly]
public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
        //this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}