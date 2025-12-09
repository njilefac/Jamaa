using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
public partial class MainMenu : UserControl
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}