using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Shared;

public partial class ModulePageHeader : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModulePageHeader, string?>(nameof(Title));

    public ModulePageHeader()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}