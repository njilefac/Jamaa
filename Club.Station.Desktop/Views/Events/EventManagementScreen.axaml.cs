using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Events;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Events
{
    public class EventManagementScreen : ReactiveUserControl<EventManagementViewModel>
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
}