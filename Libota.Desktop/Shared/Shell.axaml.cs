using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;

namespace Libota.Desktop.Shared;

[UsedImplicitly]
public partial class Shell : Window
{

    public Shell()
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