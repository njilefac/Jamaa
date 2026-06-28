using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Jamaa.Desktop.Dashboard;

public partial class WidgetView : UserControl
{
    private WidgetViewModelBase? _draggedWidget;
    private bool _isDragging;
    private Point _dragStartPoint;

    public WidgetView()
    {
        InitializeComponent();
        var widgetBorder = this.FindControl<Border>("WidgetBorder");
        if (widgetBorder is null)
            return;

        widgetBorder.PointerPressed += OnPointerPressed;
        widgetBorder.PointerMoved += OnPointerMoved;
        widgetBorder.PointerReleased += OnPointerReleased;
        widgetBorder.PointerCaptureLost += OnPointerCaptureLost;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsInteractiveChild(e.Source))
            return;

        if (DataContext is not WidgetViewModelBase widget)
            return;

        _dragStartPoint = e.GetPosition(this);
        _draggedWidget = widget;
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var currentPoint = e.GetPosition(this);
        if (Math.Abs(currentPoint.X - _dragStartPoint.X) < 6 && Math.Abs(currentPoint.Y - _dragStartPoint.Y) < 6)
            return;

        _isDragging = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
            TrySwapWithHitWidget(e);

        e.Pointer.Capture(null);
        _draggedWidget = null;
        _isDragging = false;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _draggedWidget = null;
        _isDragging = false;
    }

    private static void SwapWidgets(WidgetViewModelBase dragged, WidgetViewModelBase target)
    {
        var tempRow = dragged.Row;
        var tempCol = dragged.Column;

        dragged.Row = target.Row;
        dragged.Column = target.Column;

        target.Row = tempRow;
        target.Column = tempCol;

        if (dragged.ParentViewModel is { } vm)
            _ = vm.SaveLayout();
    }

    private void TrySwapWithHitWidget(PointerReleasedEventArgs e)
    {
        if (_draggedWidget is null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        var hit = topLevel?.InputHitTest(e.GetPosition(topLevel));
        var targetView = (hit as Visual)?.FindAncestorOfType<WidgetView>(includeSelf: true);
        var target = targetView?.DataContext as WidgetViewModelBase;

        if (ValidateDrop(_draggedWidget, target) && target is not null)
            SwapWidgets(_draggedWidget, target);
    }

    private static bool ValidateDrop(WidgetViewModelBase? dragged, WidgetViewModelBase? target)
    {
        if (dragged is null || target is null) return false;
        if (dragged == target) return false;
        if (dragged.ParentViewModel is null) return false;

        var targetBoxSize = target.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        if (dragged.AllowedBoxSize != targetBoxSize) return false;

        var draggedBoxSize = dragged.Column == 1 ? BoxSize.Wide : BoxSize.Small;
        return target.AllowedBoxSize == draggedBoxSize;
    }

    private static bool IsInteractiveChild(object? source)
    {
        return source is Visual visual &&
               (visual is Button or ListBox or MenuItem ||
                visual.FindAncestorOfType<Button>() is not null ||
                visual.FindAncestorOfType<ListBox>() is not null ||
                visual.FindAncestorOfType<MenuItem>() is not null);
    }
}
