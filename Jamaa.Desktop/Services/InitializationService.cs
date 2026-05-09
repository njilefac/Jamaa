using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Jamaa.Application.Shared.Logging;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Configuration;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Assets.Resources;
using Jamaa.Desktop.Events;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Settings;
using Jamaa.Desktop.Shared;
using Jamaa.Desktop.Configuration.Extensions;
using Jamaa.Desktop.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jamaa.Desktop.Services;

public static partial class InitializationService
{
    private static ServiceProvider? _serviceProvider;
    private static readonly BehaviorSubject<string> StatusSubject = new("Initializing application...");
    private static readonly BehaviorSubject<double> ProgressSubject = new(0);
    public static IObservable<string> Status => StatusSubject.AsObservable();
    public static IObservable<double> Progress => ProgressSubject.AsObservable();

    public static async Task<Shell> InitializeAsync(IClassicDesktopStyleApplicationLifetime lifeTime)
    {
        await UpdateStatus("Setting up logging...", 5);
        SetupLogging();

        await UpdateStatus("Building configuration...", 15);
        var configuration = BuildConfiguration();

        await UpdateStatus("Creating service provider...", 30);
        _serviceProvider = CreateServiceProvider(configuration, lifeTime);

        await UpdateStatus("Registering routes...", 45);
        RegisterRoutes(_serviceProvider.GetRequiredService<IRouteRegistry>());

        await UpdateStatus("Updating database...", 60);
        UpdateDatabaseSafely(_serviceProvider);

        await UpdateStatus("Starting background services...", 75);
        await StartBackgroundServicesAsync(_serviceProvider);

        await UpdateStatus("Setting up diagnostics...", 90);
        SetupDiagnostics(_serviceProvider);

        SetApplicationCulture();

        await UpdateStatus("Finalizing initialization...", 100);
        return CreateAndConfigureMainWindow(_serviceProvider);
    }

    public static async Task ShutdownAsync()
    {
        if (_serviceProvider == null) return;

        await SaveDashboardLayoutAsync(_serviceProvider);
        await StopBackgroundServicesAsync(_serviceProvider);
        DisposeServiceProvider(_serviceProvider);
    }

    private static void UpdateDatabaseSafely(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            UpdateDatabase(serviceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database update.");
        }
    }

    private static void SetApplicationCulture() => Messages.Culture = CultureInfo.CurrentUICulture;

    private static async Task SaveDashboardLayoutAsync(IServiceProvider serviceProvider)
    {
        var userSessionService = serviceProvider.GetService<IUserSessionService>();
        if (userSessionService?.CurrentUserSession?.IsAuthenticated != true) return;

        var dashboard = serviceProvider.GetService<DashboardViewModel>();
        if (dashboard != null)
        {
            await dashboard.SaveLayout();
        }
    }

    private static async Task StopBackgroundServicesAsync(IServiceProvider serviceProvider)
    {
        var akkaService = serviceProvider.GetService<IHostedService>();
        if (akkaService != null)
        {
            // Stop background services but avoid UI thread dependencies during disposal if not on UI thread
            await akkaService.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static void DisposeServiceProvider(IServiceProvider? serviceProvider)
    {
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static async Task UpdateStatus(string status, double progress)
    {
        StatusSubject.OnNext(status);
        ProgressSubject.OnNext(progress);
        await Task.Yield();
    }

    private static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With<SessionUserNameEnricher>()
            .WriteTo.Console()
            .CreateLogger();
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var baseDir = AppContext.BaseDirectory;
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
            ?? Environment.GetEnvironmentVariable("Environment") 
            ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appSettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static ServiceProvider CreateServiceProvider(IConfigurationRoot configuration,
        IClassicDesktopStyleApplicationLifetime lifeTime)
    {
        return new ServiceCollection()
            .ConfigureServices(configuration)
            .ConfigureAkka(lifeTime, configuration)
            .BuildServiceProvider();
    }

    private static async Task StartBackgroundServicesAsync(IServiceProvider serviceProvider)
    {
        var akkaService = serviceProvider.GetRequiredService<IHostedService>();
        await akkaService.StartAsync(CancellationToken.None);
    }

    private static void SetupDiagnostics(IServiceProvider serviceProvider)
    {
        var diagnosticListener = serviceProvider.GetService<IObserver<DiagnosticListener>>();
        if (diagnosticListener != null)
            DiagnosticListener.AllListeners.Subscribe(diagnosticListener);
    }

    private static void RegisterRoutes(IRouteRegistry routes)
    {
        routes.Register(new RouteMap(Path: Routes.Shell, ViewModel: typeof(ShellViewModel), Nested:
        [
            new RouteMap(Path: Routes.CreateOrganisation, ViewModel: typeof(Setup.CreateOrganisationViewModel)),
            new RouteMap(Path: Routes.CreateSuperUser, ViewModel: typeof(Setup.CreateSuperUserViewModel)),
            new RouteMap(Path: Routes.Login, ViewModel: typeof(Security.LoginScreenViewModel)),
            new RouteMap(Path: Routes.Home, ViewModel: typeof(MainWindowViewModel), Nested:
            [
                new RouteMap(Path: Routes.Dashboard, ViewModel: typeof(DashboardViewModel), IsDefault: true),
                new RouteMap(Path: Routes.MembersOverview, ViewModel: typeof(Members.Pages.MembersOverviewViewModel), Nested:
                    [
                        new RouteMap(Path: Routes.MembersList, ViewModel: typeof(Members.Components.MemberListViewModel)),
                        new RouteMap(Path: Routes.MemberProfile, ViewModel: typeof(Members.Pages.MemberProfileViewModel)),
                    ]
                ),
                new RouteMap(Path: Routes.EventsOverview, ViewModel: typeof(EventsOverviewPageViewModel)),
                new RouteMap(Path: Routes.AccountingOverview, ViewModel: typeof(AccountingDashboardViewModel)),
                new RouteMap(Path: Routes.AccountingDashboard, ViewModel: typeof(AccountingDashboardViewModel)),
                new RouteMap(Path: Routes.AccountingTransactions, ViewModel: typeof(JournalEntriesViewModel)),
                new RouteMap(Path: Routes.BankReconciliation, ViewModel: typeof(BankReconciliationViewModel)),
                new RouteMap(Path: Routes.AccountingReports, ViewModel: typeof(AccountingReportsViewModel)),
                new RouteMap(Path: Routes.Settings, ViewModel: typeof(SettingsViewModel), Nested:
                [
                    new RouteMap(Path: Routes.AccountingConfiguration, ViewModel: typeof(AccountingConfigurationViewModel), Nested:
                    [
                        new RouteMap(Path: Routes.AccountingCurrencyAndDateFormats, ViewModel: typeof(AccountingCurrencyAndDateFormatsViewModel)),
                        new RouteMap(Path: Routes.FiscalCalendarAndPeriods, ViewModel: typeof(FiscalCalendarAndPeriodsViewModel)),
                        new RouteMap(Path: Routes.ChartOfAccounts, ViewModel: typeof(ChartOfAccountsViewModel)),
                        new RouteMap(Path: Routes.TaxGroupsAndAuthorities, ViewModel: typeof(TaxGroupsAndAuthoritiesViewModel)),
                        new RouteMap(Path: Routes.AutomationRules, ViewModel: typeof(AutomationRulesViewModel)),
                        new RouteMap(Path: Routes.UserRolesAndApprovals, ViewModel: typeof(UserRolesAndApprovalsViewModel)),
                        new RouteMap(Path: Routes.OpeningBalancesAndMigration, ViewModel: typeof(OpeningBalancesAndMigrationViewModel)),
                        new RouteMap(Path: Routes.AccountLedger, ViewModel: typeof(AccountLedgerViewModel)),
                    ]),
                    new RouteMap(Path: Routes.EventsConfiguration, ViewModel: typeof(EventsConfigurationViewModel)),
                ]),
            ]),
        ]));
    }

    private static Shell CreateAndConfigureMainWindow(ServiceProvider serviceProvider)
    {
        var mainWindow = (serviceProvider ?? throw new InvalidOperationException())
            .GetRequiredService<Shell>();
        var mainViewModel = serviceProvider.GetRequiredService<ShellViewModel>();
        mainWindow.DataContext = mainViewModel;
        return mainWindow;
    }

    private static void UpdateDatabase(IServiceProvider serviceProvider, ILogger<Program> logger)
    {
        var dataContext = serviceProvider.GetService<JamaaDbContext>();
        if (dataContext == null) return;

        var pendingMigrations = GetPendingMigrations(dataContext);
        if (pendingMigrations.Length != 0)
        {
            ApplyMigrations(dataContext, logger, pendingMigrations);
        }
        else
        {
            LogDatabaseIsUpToDate(logger);
        }
    }

    private static string[] GetPendingMigrations(JamaaDbContext dataContext) => 
        dataContext.Database.GetPendingMigrations().ToArray();

    private static void ApplyMigrations(JamaaDbContext dataContext, ILogger<Program> logger, string[] pendingMigrations)
    {
        LogApplyingPendingMigrations(logger, string.Join(", ", pendingMigrations));
        dataContext.Database.Migrate();
        LogTheDatabaseWasUpgraded(logger);
    }

    // Since LoggerMessage is partial, we'd need to put this in a partial class 
    // or just use logger.LogError directly for simplicity if it's not a performance-critical path.
    // However, I will keep it simple for now or use the logger directly.
    private static void LogException(ILogger logger) => logger.LogError("An error occurred during database update.");
    private static void LogApplyingPendingMigrations(ILogger logger, string migrations) => logger.LogApplyingPendingMigrationsMigrations(migrations);
    private static void LogTheDatabaseWasUpgraded(ILogger logger) => logger.LogDebug("The database was upgraded");
    private static void LogDatabaseIsUpToDate(ILogger logger) => logger.LogDebug("Database is up-to-date!");
    [LoggerMessage(LogLevel.Debug, "Applying pending migrations: [{migrations}]")]
    static partial void LogApplyingPendingMigrationsMigrations(this ILogger logger, string migrations);
}
