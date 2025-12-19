using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Views.Shared;

public partial class Dashboard : UserControl
{
    public Dashboard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}