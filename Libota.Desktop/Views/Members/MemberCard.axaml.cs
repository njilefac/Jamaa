using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberCard : UserControl
{
    public MemberCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}