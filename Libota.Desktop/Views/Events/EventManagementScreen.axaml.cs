using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Events;

namespace Libota.Desktop.Views.Events;

[SingleInstanceView]
public partial class EventManagementScreen : UserControl
{
    public EventManagementScreen(EventManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new EventManagementViewModel? DataContext { get; set; }
}