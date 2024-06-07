using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Events;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Events;

[SingleInstanceView]
public partial class EventManagementScreen : ReactiveUserControl<EventManagementViewModel>
{
    public EventManagementScreen()
    {
        InitializeComponent();
        DataContext = Locator.Current.GetService<EventManagementViewModel>();
        this.WhenActivated(disposables => { });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}