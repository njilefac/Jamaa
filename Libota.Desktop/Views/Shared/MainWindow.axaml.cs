using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
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
            var contentPageContainer = this.FindControl<RoutedViewHost>("ContentContainer");
            if (contentPageContainer != null)
            {
                contentPageContainer.DefaultContent = GetStartupScreen().Result;
            }
        }

        private static async Task<IViewFor?> GetStartupScreen()
        {
            var setupService = Locator.Current.GetService<ISetupService>();
            var existingOrganisations = await setupService?.ListOrganisations()!;

            if (existingOrganisations.Any() == false)
                return Locator.Current.GetService<IViewFor<CreateOrganisationViewModel>>();

            var superUser = await setupService.GetSuperUser();
            if (superUser == null)
                return Locator.Current.GetService<IViewFor<CreateSuperUserViewModel>>();

            return Locator.Current.GetService<IViewFor<LoginScreenViewModel>>();
        }
    }
}