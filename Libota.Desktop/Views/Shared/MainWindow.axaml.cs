using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Application.Setup;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
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
        //this.AttachDevTools();
#endif
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Screens.Primary == null) return;
        var screenSize = Screens.Primary.WorkingArea.Size;
        var windowSize = PixelSize.FromSize(ClientSize, Screens.Primary.Scaling);

        Position = new PixelPoint(
            screenSize.Width - windowSize.Width,
            screenSize.Height - windowSize.Height);
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