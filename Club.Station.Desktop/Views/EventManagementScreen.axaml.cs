using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views
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