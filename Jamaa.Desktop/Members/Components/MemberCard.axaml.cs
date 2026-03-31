using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCard : UserControl
{
    private bool _isPressed;
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MemberCard, bool>(nameof(IsSelected));

    static MemberCard()
    {
        IsSelectedProperty.Changed.AddClassHandler<MemberCard>((x, e) => x.OnIsSelectedChanged(e));
    }

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Focus();
        }
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public MemberCard()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPressed = true;
            PseudoClasses.Set(":pressed", true);
            e.Pointer.Capture(this);
            e.Handled = false; // Allow ListBoxItem to see the event for selection
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            PseudoClasses.Set(":pressed", false);
            e.Pointer.Capture(null);
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            PseudoClasses.Set(":pressed", false);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}