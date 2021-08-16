using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views
{
    public partial class MainNavigation : ReactiveUserControl<MainNavigationViewModel>
    {
        public MainNavigation()
        {
            DataContext = Locator.Current.GetService<MainNavigationViewModel>();
            this.WhenActivated(disposables => { });
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}