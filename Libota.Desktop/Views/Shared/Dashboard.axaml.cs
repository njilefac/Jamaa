using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared
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