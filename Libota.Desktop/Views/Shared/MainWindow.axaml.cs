using System;
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
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Libota.Desktop.Views.Shared
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private readonly IServiceProvider _serviceProvider;
        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            this.WhenActivated(disposables =>
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
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

        private  async Task<IViewFor?> GetStartupScreen()
        {
            var setupService = _serviceProvider.GetService<ISetupService>();
            if (setupService == null)
                return _serviceProvider.GetService<CreateOrganisationScreen>();

            var organisations = await setupService.ListOrganisations();
            if (!organisations.Any())
                return _serviceProvider.GetService<CreateOrganisationScreen>();

            var superUser = setupService.GetSuperUser().Result;
            if (superUser == null)
                return _serviceProvider.GetService<CreateSuperUserScreen>();

            return _serviceProvider.GetService<LoginScreen>();
        }
    }
}