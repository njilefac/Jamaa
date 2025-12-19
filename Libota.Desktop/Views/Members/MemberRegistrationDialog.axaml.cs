using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.Infrastructure.Attributes;

namespace Libota.Desktop.Views.Members;

[SingleInstanceView]
public partial class MemberRegistrationDialog : ContentDialog
{
    public MemberRegistrationDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}