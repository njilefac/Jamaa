using Avalonia;
using Avalonia.Controls;

using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Shared;

public partial class DashboardHeader : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DashboardHeader, string?>(nameof(Title));

    public DashboardHeader()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}