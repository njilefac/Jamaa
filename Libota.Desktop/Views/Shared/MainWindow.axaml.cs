using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
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
                DataContext = serviceProvider.GetRequiredKeyedService<MainWindowViewModel>("MainWindow");
                Disposable.Create(() => { }).DisposeWith(disposables);
            });

            InitializeComponent();
//#if DEBUG
//            this.AttachDevTools();
//#endif
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

        private  async Task<IViewFor?> GetStartupScreen()
        {
            var setupService = _serviceProvider.GetRequiredService<ISetupService>();
            var existingOrganisations = await setupService.ListOrganisations();
            
            if(existingOrganisations.Any() == false)
                return _serviceProvider.GetRequiredService<CreateOrganisationScreen>();

            var superUser = await setupService.GetSuperUser();
            if (superUser == null)
                return _serviceProvider.GetRequiredService<CreateSuperUserScreen>();

            return _serviceProvider.GetRequiredService<LoginScreen>();
        }
    }
}