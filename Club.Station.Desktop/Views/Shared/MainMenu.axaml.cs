using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Club.Station.Desktop.Views.Shared
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