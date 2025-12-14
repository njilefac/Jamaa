using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberRegistrationModal : Modal
{
    public MemberRegistrationModal()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}