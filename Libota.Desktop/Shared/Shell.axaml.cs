using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using JetBrains.Annotations;

namespace Libota.Desktop.Shared;

[UsedImplicitly]
public partial class Shell : AppWindow
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