using System;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Huskui.Avalonia.Controls;
using Jamaa.Desktop.Members.ViewModels;
using Jamaa.Desktop.Members.Messages;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCard : UserControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MemberCard, bool>(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private MemberListViewModel? GetViewModel() => this.FindAncestorOfType<MembersList>()?.DataContext as MemberListViewModel;

    public MemberCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}