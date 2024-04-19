using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared
{
    public partial class MainMenu : ReactiveUserControl<MainMenuViewModel>
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