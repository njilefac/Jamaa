using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.Views.Security;
using Libota.Desktop.Views.Setup;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            this.WhenActivated(disposables =>
            {
                DataContext = Locator.Current.GetService<MainWindowViewModel>();
                Disposable.Create(() => { }).DisposeWith(disposables);
            });

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var contentPageContainer = this.FindControl<RoutedViewHost>("ContentPageContainer");
            if (contentPageContainer != null)
            {
                contentPageContainer.DefaultContent = GetStartupScreen().Result;
            }
        }

        private static async Task<IViewFor?> GetStartupScreen()
        {
            var setupService = Locator.Current.GetService<ISetupService>();
            if (setupService == null)
                return Locator.Current.GetService<CreateOrganisationScreen>();

            var organisations = await setupService.ListOrganisations();
            if (!organisations.Any())
                return Locator.Current.GetService<CreateOrganisationScreen>();

            var superUser = setupService.GetSuperUser().Result;
            if (superUser == null)
                return Locator.Current.GetService<CreateSuperUserScreen>();

            return Locator.Current.GetService<LoginScreen>();
        }
    }
}