using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Navigation;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
public partial class Dashboard : UserControl, IViewFor<DashboardViewModel>
{
    public Dashboard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new DashboardViewModel? DataContext { get; set; }
}