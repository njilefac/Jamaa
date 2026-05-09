using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Shared;

public partial class OperationStatusBanner : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<OperationStatusBanner, bool>(nameof(IsActive));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<OperationStatusBanner, string?>(nameof(Message), "Waiting for confirmation...");

    public OperationStatusBanner()
    {
        InitializeComponent();
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}