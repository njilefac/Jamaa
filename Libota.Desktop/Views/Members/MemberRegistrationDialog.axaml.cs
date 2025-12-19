using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;

namespace Libota.Desktop.Views.Members;

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