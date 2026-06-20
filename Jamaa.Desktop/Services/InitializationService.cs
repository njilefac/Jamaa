using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
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
using System.IO;
using System.Reflection;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using Jamaa.Desktop.Services.Updater;
using Syncfusion.Licensing;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jamaa.Desktop.Services;

public static partial class InitializationService
{
    private static ServiceProvider? _serviceProvider;
    private static SparkleUpdater? _sparkle;
    private static IClassicDesktopStyleApplicationLifetime? _lifeTime;
    private static readonly BehaviorSubject<string> StatusSubject = new("Getting Jamaa ready...");
    private static readonly BehaviorSubject<double> ProgressSubject = new(0);
    public static IObservable<string> Status => StatusSubject.AsObservable();
    public static IObservable<double> Progress => ProgressSubject.AsObservable();
    public static ServiceProvider? ServiceProvider => _serviceProvider;

    public static async Task<Shell> InitializeAsync(IClassicDesktopStyleApplicationLifetime lifeTime)
    {
        _lifeTime = lifeTime;
        TraceStartup("InitializeAsync:start");
        await UpdateStatus("Preparing your workspace...", 20);
        TraceStartup("SetupLogging");
        SetupLogging();

        TraceStartup("BuildConfiguration");
        var configuration = BuildConfiguration();

        TraceStartup("CreateServiceProvider");
        _serviceProvider = CreateServiceProvider(configuration, lifeTime);

        TraceStartup("StartEmbeddedWebServerAsync");
        var embeddedWebServerTask = StartEmbeddedWebServerAsync(_serviceProvider);

        await UpdateStatus("Loading features...", 45);
        TraceStartup("InitializeSyncfusionLicense");
        InitializeSyncfusionLicense(_serviceProvider);

        TraceStartup("RegisterRoutes");
        RegisterRoutes(_serviceProvider.GetRequiredService<IRouteRegistry>());

        await UpdateStatus("Starting services...", 70);
        TraceStartup("StartActorServicesAsync");
        await StartActorServicesAsync(_serviceProvider);

        await UpdateStatus("Almost ready...", 90);
        TraceStartup("SetApplicationCulture");
        SetApplicationCulture();

        await UpdateStatus("Opening Jamaa...", 100);
        TraceStartup("CreateAndConfigureMainWindow");
        var mainWindow = await Dispatcher.UIThread.InvokeAsync(() => CreateAndConfigureMainWindow(_serviceProvider));
        _ = RunDeferredStartupAsync(_serviceProvider, embeddedWebServerTask);
        TraceStartup("InitializeAsync:done");
        mainWindow.Opened += (s, e) => CheckForUpdates(_serviceProvider!);
        return mainWindow;
    }

    public static async Task ShutdownAsync()
    {
        var serviceProvider = _serviceProvider;
        if (serviceProvider == null) return;

        _serviceProvider = null;

        _sparkle?.StopLoop();
        _sparkle = null;

        await SaveDashboardLayoutAsync(serviceProvider);
        await StopBackgroundServicesAsync(serviceProvider);
        await DisposeServiceProviderAsync(serviceProvider);
    }

    private static void UpdateDatabaseSafely(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var logger = scopedProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            UpdateDatabase(scopedProvider, logger);
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
        Log.Information("Startup status: {Status} ({Progress}%)", status, progress);
        StatusSubject.OnNext(status);
        ProgressSubject.OnNext(progress);
        await Task.Yield();
    }

    private static void TraceStartup(string step)
    {
        Log.Information("Startup step: {Step}", step);
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
            .AddJsonFile("appSettings.json", false, true)
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

    private static async Task StartActorServicesAsync(IServiceProvider serviceProvider)
    {
        var akkaService = serviceProvider.GetRequiredService<IHostedService>();
        await akkaService.StartAsync(CancellationToken.None);
    }

    private static async Task ObserveBackgroundServicesAsync(Task embeddedWebServerTask)
    {
        try
        {
            await embeddedWebServerTask.ConfigureAwait(false);
            Log.Information("Embedded web server started.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start embedded web server.");
        }
    }

    private static async Task RunDeferredStartupAsync(IServiceProvider serviceProvider, Task embeddedWebServerTask)
    {
        await UpdateStatus("Final checks...", 60);
        TraceStartup("UpdateDatabaseSafely");
        UpdateDatabaseSafely(serviceProvider);

        await UpdateStatus("Starting background tasks...", 75);
        TraceStartup("ObserveBackgroundServicesAsync");
        _ = ObserveBackgroundServicesAsync(embeddedWebServerTask);

        await UpdateStatus("Finishing setup...", 90);
        TraceStartup("SetupDiagnostics");
        SetupDiagnostics(serviceProvider);
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
            _sparkle = new SparkleUpdater(settings.UpdateUrl, new DSAChecker(SecurityMode.Unsafe))
            {
                UIFactory = new JamaaUiFactory(),
                RelaunchAfterUpdate = true,
            };
            _sparkle.AppCastHelper.AppCastFilter = new InstalledVersionAppCastFilter();

            _sparkle.CloseApplication += () =>
            {
                _lifeTime?.Shutdown();
            };

            _sparkle.StartLoop(true, true, TimeSpan.FromMinutes(settings.UpdateIntervalMinutes));
            logger.LogInformation("Update check initiated with interval of {Interval} hours.", settings.UpdateIntervalMinutes);
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
