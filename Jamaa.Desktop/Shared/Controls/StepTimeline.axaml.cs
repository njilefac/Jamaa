using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Jamaa.Desktop.Shared.Controls;

public partial class StepTimeline : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<StepTimeline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<StepTimeline, int>(nameof(SelectedIndex), defaultValue: 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<StepTimeline, Orientation>(nameof(Orientation), defaultValue: Orientation.Horizontal);

    public static readonly StyledProperty<bool> IsHorizontalProperty =
        AvaloniaProperty.Register<StepTimeline, bool>(nameof(IsHorizontal), defaultValue: true);

    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<StepTimeline, bool>(nameof(IsVertical));

    public static readonly StyledProperty<bool> ShowStepTitleProperty =
        AvaloniaProperty.Register<StepTimeline, bool>(nameof(ShowStepTitle), defaultValue: true);

    public static readonly StyledProperty<bool> ShowStepDescriptionProperty =
        AvaloniaProperty.Register<StepTimeline, bool>(nameof(ShowStepDescription), defaultValue: true);

    public StepTimeline()
    {
        InitializeComponent();
        UpdateOrientationFlags(Orientation);
        this.GetObservable(OrientationProperty).Subscribe(new OrientationObserver(this));
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public bool IsHorizontal
    {
        get => GetValue(IsHorizontalProperty);
        set => SetValue(IsHorizontalProperty, value);
    }

    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    public bool ShowStepTitle
    {
        get => GetValue(ShowStepTitleProperty);
        set => SetValue(ShowStepTitleProperty, value);
    }

    public bool ShowStepDescription
    {
        get => GetValue(ShowStepDescriptionProperty);
        set => SetValue(ShowStepDescriptionProperty, value);
    }

    private void UpdateOrientationFlags(Orientation orientation)
    {
        var isHorizontal = orientation == Orientation.Horizontal;
        SetCurrentValue(IsHorizontalProperty, isHorizontal);
        SetCurrentValue(IsVerticalProperty, !isHorizontal);
    }

    private sealed class OrientationObserver(StepTimeline owner) : IObserver<Orientation>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Orientation value)
        {
            owner.UpdateOrientationFlags(value);
        }
    }
}






