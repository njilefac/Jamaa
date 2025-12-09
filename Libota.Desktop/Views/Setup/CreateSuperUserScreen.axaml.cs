using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Setup;

namespace Libota.Desktop.Views.Setup;

[SingleInstanceView]
public partial class CreateSuperUserScreen : UserControl
{
    public CreateSuperUserScreen()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var nameField = this.FindControl<TextBox>("UserNameField");
        nameField!.AttachedToVisualTree += (target, _) => (target as TextBox)!.Focus();
    }
}