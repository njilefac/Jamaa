using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Views.Shared;

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