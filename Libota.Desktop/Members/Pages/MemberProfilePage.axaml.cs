using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Members.Pages;

public partial class MemberProfilePage : UserControl
{
    public MemberProfilePage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}