using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Libota.Desktop.Setup;

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