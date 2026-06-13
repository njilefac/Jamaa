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
using Jamaa.Desktop.Configuration;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Assets.Resources;
using Jamaa.Desktop.Configuration.Extensions;
using Jamaa.Desktop.Accounting.Wizard;
using Jamaa.Desktop.Dashboard;
using Jamaa.Desktop.Events;
using Jamaa.Desktop.Members.Components;
using Jamaa.Desktop.Members.Pages;
using Jamaa.Desktop.Security;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Services.Hosting;
using Jamaa.Desktop.Settings;
using Jamaa.Desktop.Setup;
using Jamaa.Desktop.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.Avalonia;
using Syncfusion.Licensing;
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
    public static ServiceProvider? ServiceProvider => _serviceProvider;

    public static async Task<Shell> InitializeAsync(IClassicDesktopStyleApplicationLifetime lifeTime)
    {
        await UpdateStatus("Setting up logging...", 5);
        SetupLogging();

        await UpdateStatus("Building configuration...", 15);
        var configuration = BuildConfiguration();

        await UpdateStatus("Creating service provider...", 30);
        _serviceProvider = CreateServiceProvider(configuration, lifeTime);

        var embeddedWebServerTask = StartEmbeddedWebServerAsync(_serviceProvider);

        await UpdateStatus("Verifying licenses...", 40);
        InitializeSyncfusionLicense(_serviceProvider);

        await UpdateStatus("Registering routes...", 45);
        RegisterRoutes(_serviceProvider.GetRequiredService<IRouteRegistry>());

        await UpdateStatus("Updating database...", 60);
        UpdateDatabaseSafely(_serviceProvider);

        await UpdateStatus("Starting background services...", 75);
        await StartBackgroundServicesAsync(_serviceProvider, embeddedWebServerTask);

        await UpdateStatus($"started embedded server", 80);

        await UpdateStatus("Setting up diagnostics...", 90);
        SetupDiagnostics(_serviceProvider);

        SetApplicationCulture();

        await UpdateStatus("Finalizing initialization...", 100);
        var mainWindow = CreateAndConfigureMainWindow(_serviceProvider);
        mainWindow.Opened += (s, e) => CheckForUpdates(_serviceProvider!);
        return mainWindow;
    }

    public static async Task ShutdownAsync()
    {
        var serviceProvider = _serviceProvider;
        if (serviceProvider == null) return;

        _serviceProvider = null;

        await SaveDashboardLayoutAsync(serviceProvider);
        await StopBackgroundServicesAsync(serviceProvider);
        await DisposeServiceProviderAsync(serviceProvider);
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

    private static void SetApplicationCulture()
    {
        Messages.Culture = CultureInfo.CurrentUICulture;
    }

    private static async Task SaveDashboardLayoutAsync(IServiceProvider serviceProvider)
    {
        var userSessionService = serviceProvider.GetService<IUserSessionService>();
        if (userSessionService?.CurrentUserSession?.IsAuthenticated != true) return;

        var dashboard = serviceProvider.GetService<DashboardViewModel>();
        if (dashboard != null) await dashboard.SaveLayout();
    }

    private static async Task StopBackgroundServicesAsync(IServiceProvider serviceProvider)
    {
        var embeddedWebServer = serviceProvider.GetService<IEmbeddedWebServer>();
        if (embeddedWebServer != null)
            await embeddedWebServer.StopAsync(CancellationToken.None).ConfigureAwait(false);

        var akkaService = serviceProvider.GetService<IHostedService>();
        if (akkaService != null)
            // Stop background services but avoid UI thread dependencies during disposal if not on UI thread
            await akkaService.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task DisposeServiceProviderAsync(IServiceProvider? serviceProvider)
    {
        switch (serviceProvider)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                return;
            case IDisposable disposable:
                disposable.Dispose();
                break;
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
            .AddJsonFile("appSettings.json", true, true)
            .AddJsonFile($"appSettings.{environment}.json", true, true)
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

    private static Task StartEmbeddedWebServerAsync(IServiceProvider serviceProvider)
    {
        var embeddedWebServer = serviceProvider.GetRequiredService<IEmbeddedWebServer>();
        return embeddedWebServer.StartAsync(CancellationToken.None);
    }

    private static async Task StartBackgroundServicesAsync(IServiceProvider serviceProvider, Task embeddedWebServerTask)
    {
        var akkaService = serviceProvider.GetRequiredService<IHostedService>();
        await akkaService.StartAsync(CancellationToken.None);

        await embeddedWebServerTask.ConfigureAwait(false);
    }

    private static void InitializeSyncfusionLicense(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var settings = serviceProvider.GetRequiredService<IOptions<SyncfusionSettings>>().Value;

        if (string.IsNullOrWhiteSpace(settings.LicenseKey))
        {
            logger.LogWarning("Syncfusion license key is not configured.");
            return;
        }

        SyncfusionLicenseProvider.RegisterLicense(settings.LicenseKey);
        logger.LogInformation("Syncfusion license initialized.");
    }

    private static void CheckForUpdates(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var settings = serviceProvider.GetRequiredService<IOptions<UpdaterSettings>>().Value;

        if (string.IsNullOrWhiteSpace(settings.UpdateUrl))
        {
            logger.LogWarning("Update URL is not configured.");
            return;
        }

        try
        {
            var sparkle = new SparkleUpdater(settings.UpdateUrl, new DSAChecker(SecurityMode.UseIfPossible))
            {
                UIFactory = new UIFactory {},
                RelaunchAfterUpdate = true,
                
            };
            sparkle.StartLoop(true, true, TimeSpan.FromHours(settings.UpdateIntervalHours));
            logger.LogInformation("Update check initiated with interval of {Interval} hours.", settings.UpdateIntervalHours);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate update check.");
        }
    }

    private static void SetupDiagnostics(IServiceProvider serviceProvider)
    {
        var diagnosticListener = serviceProvider.GetService<IObserver<DiagnosticListener>>();
        if (diagnosticListener != null)
            DiagnosticListener.AllListeners.Subscribe(diagnosticListener);
    }

    private static void RegisterRoutes(IRouteRegistry routes)
    {
        routes.Register(new RouteMap(Routes.Shell, typeof(ShellViewModel), Nested:
        [
            new RouteMap(Routes.CreateOrganisation, typeof(CreateOrganisationViewModel)),
            new RouteMap(Routes.CreateSuperUser, typeof(CreateSuperUserViewModel)),
            new RouteMap(Routes.Login, typeof(LoginScreenViewModel)),
            new RouteMap(Routes.Home, typeof(MainWindowViewModel), Nested:
            [
                new RouteMap(Routes.Dashboard, typeof(DashboardViewModel), true),
                new RouteMap(Routes.MembersOverview, typeof(MembersOverviewViewModel), Nested:
                    [
                        new RouteMap(Routes.MembersList, typeof(MemberListViewModel)),
                        new RouteMap(Routes.MemberProfile, typeof(MemberProfileViewModel))
                    ]
                ),
                new RouteMap(Routes.EventsOverview, typeof(EventsOverviewPageViewModel)),
                new RouteMap(Routes.AccountingOverview, typeof(AccountingDashboardViewModel)),
                new RouteMap(Routes.AccountingDashboard, typeof(AccountingDashboardViewModel)),
                new RouteMap(Routes.AccountingTransactions, typeof(JournalEntriesViewModel)),
                new RouteMap(Routes.BankReconciliation, typeof(BankReconciliationViewModel)),
                new RouteMap(Routes.AccountingReports, typeof(AccountingReportsViewModel)),
                new RouteMap(Routes.Settings, typeof(SettingsViewModel), Nested:
                [
                    new RouteMap(Routes.AccountingConfiguration, typeof(AccountingConfigurationViewModel), Nested:
                    [
                        new RouteMap(Routes.AccountingCurrencyAndDateFormats,
                            typeof(AccountingCurrencyAndDateFormatsViewModel)),
                        new RouteMap(Routes.FiscalCalendarAndPeriods, typeof(FiscalCalendarAndPeriodsViewModel)),
                        new RouteMap(Routes.ChartOfAccounts, typeof(ChartOfAccountsViewModel)),
                        new RouteMap(Routes.TaxGroupsAndAuthorities, typeof(TaxGroupsAndAuthoritiesViewModel)),
                        new RouteMap(Routes.AutomationRules, typeof(AutomationRulesViewModel)),
                        new RouteMap(Routes.UserRolesAndApprovals, typeof(UserRolesAndApprovalsViewModel)),
                        new RouteMap(Routes.AccountingSetupWizard, typeof(AccountingSetupWizardViewModel)),
                        new RouteMap(Routes.AccountLedger, typeof(AccountLedgerViewModel))
                    ]),
                    new RouteMap(Routes.EventsConfiguration, typeof(EventsConfigurationViewModel))
                ])
            ])
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

        // Enable WAL mode and set busy timeout for better concurrency
        dataContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        dataContext.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");

        var pendingMigrations = GetPendingMigrations(dataContext);
        if (pendingMigrations.Length != 0)
            ApplyMigrations(dataContext, logger, pendingMigrations);
        else
            LogDatabaseIsUpToDate(logger);
    }

    private static string[] GetPendingMigrations(JamaaDbContext dataContext)
    {
        return dataContext.Database.GetPendingMigrations().ToArray();
    }

    private static void ApplyMigrations(JamaaDbContext dataContext, ILogger<Program> logger, string[] pendingMigrations)
    {
        LogApplyingPendingMigrations(logger, string.Join(", ", pendingMigrations));
        dataContext.Database.Migrate();
        LogTheDatabaseWasUpgraded(logger);
    }

    private static void LogApplyingPendingMigrations(ILogger logger, string migrations)
    {
        logger.LogApplyingPendingMigrationsMigrations(migrations);
    }

    private static void LogTheDatabaseWasUpgraded(ILogger logger)
    {
        logger.LogDebug("The database was upgraded");
    }

    private static void LogDatabaseIsUpToDate(ILogger logger)
    {
        logger.LogDebug("Database is up-to-date!");
    }

    [LoggerMessage(LogLevel.Debug, "Applying pending migrations: [{migrations}]")]
    static partial void LogApplyingPendingMigrationsMigrations(this ILogger logger, string migrations);
}
