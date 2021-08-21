using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Shared
{
    public class Dashboard : ReactiveUserControl<DashboardViewModel>
    {
        public Dashboard()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetService<DashboardViewModel>();
            this.WhenActivated(disposables => { });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}