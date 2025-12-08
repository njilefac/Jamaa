using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.Views.Shared;

[SingleInstanceView]
[UsedImplicitly]
public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
{
    private readonly ISetupService _setupService;
    private readonly IViewFor<CreateOrganisationViewModel> _createOrganisationView;
    private readonly IViewFor<LoginScreenViewModel> _loginScreenView;
    private readonly IViewFor<CreateSuperUserViewModel> _createSuperUserView;

    public MainWindow(MainWindowViewModel viewModel,
        ISetupService setupService,
        IViewFor<CreateOrganisationViewModel> createOrganisationView,
        IViewFor<CreateSuperUserViewModel> createSuperUserView,
        IViewFor<LoginScreenViewModel> loginScreenView)
    {
        DataContext = viewModel;
        
        _setupService = setupService;
        _createOrganisationView = createOrganisationView;
        _loginScreenView = loginScreenView;
        _createSuperUserView = createSuperUserView;

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
        var contentPageContainer = this.FindControl<ContentControl>("ContentContainer");
        contentPageContainer?.Content = GetStartupScreen().Result;
    }

    private async Task<UserControl?> GetStartupScreen()
    {
        var existingOrganisations = await _setupService.ListOrganisations();

        if (!existingOrganisations.Any())
            return _createOrganisationView as UserControl;

        var superUser = await _setupService.GetSuperUser();
        if (superUser == null)
            return _createSuperUserView as UserControl;

        return _loginScreenView as UserControl;
    }

    public new MainWindowViewModel? DataContext { get; set; }
}