using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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

    public MemberCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}