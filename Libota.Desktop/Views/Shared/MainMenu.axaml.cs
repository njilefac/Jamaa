using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
public partial class MainMenu : UserControl, IViewFor<MainMenuViewModel>
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new MainMenuViewModel? DataContext { get; set; }
}