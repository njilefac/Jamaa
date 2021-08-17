using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views
{
    public class MainMenu : ReactiveUserControl<MainMenuViewModel>
    {
        public MainMenu()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
            });
            DataContext = Locator.Current.GetService<MainMenuViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}