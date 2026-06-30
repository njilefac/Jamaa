using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Members.ViewModels;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCard : UserControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MemberCard, bool>(nameof(IsSelected));

    private bool _isPressed;

    static MemberCard()
    {
        IsSelectedProperty.Changed.AddClassHandler<MemberCard>((x, e) => x.OnIsSelectedChanged(e));
    }

    public MemberCard()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true) Focus();
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ShowMemberProfile();
        e.Handled = true;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        ShowMemberProfile();
        e.Handled = true;
    }

    private void ShowMemberProfile()
    {
        if (DataContext is not MemberViewModel member)
            return;

        var membersList = this.FindAncestorOfType<MembersList>();
        if (membersList?.DataContext is not MemberListViewModel vm)
            return;

        var args = new MemberProfileNavigationArgs(MemberListViewModel.MapToData(member), "General");
        if (vm.ShowMemberProfileCommand.CanExecute(args))
            vm.ShowMemberProfileCommand.Execute(args);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
