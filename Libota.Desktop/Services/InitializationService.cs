using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Libota.Application.Shared.Logging;
using Libota.Data.Configuration;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Configuration.Extensions;
using Libota.Desktop.Events;
using Libota.Desktop.Finances;
using Libota.Desktop.Services.Navigation.Interfaces;
using Libota.Desktop.Services.Navigation.Models;
using Libota.Desktop.Services.Navigation.Values;
using Libota.Desktop.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Libota.Desktop.Services;

public static class InitializationService
{
    public static async Task<Shell> InitializeAsync(IClassicDesktopStyleApplicationLifetime lifeTime)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With<SessionUserNameEnricher>()
            .WriteTo.Console()
            .CreateLogger();

        var environment = Environment.GetEnvironmentVariable("Environment") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appSettings.{environment}.json", false, true)
            .AddEnvironmentVariables()
            .Build();

        var serviceProvider = new ServiceCollection()
            .ConfigureServices(configuration)
            .ConfigureAkka(lifeTime, configuration)
            .BuildServiceProvider();

        var routes = serviceProvider.GetRequiredService<IRouteRegistry>();
        RegisterRoutes(routes);

        var akkaService = serviceProvider.GetRequiredService<IHostedService>();
        await akkaService.StartAsync(CancellationToken.None);

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            UpdateDatabase(logger, serviceProvider);
            var diagnosticListener = serviceProvider.GetService<IObserver<DiagnosticListener>>();
            if (diagnosticListener != null)
                DiagnosticListener.AllListeners.Subscribe(diagnosticListener);
        }
        catch (Exception)
        {
            LogException(logger);
        }

        Messages.Culture = CultureInfo.CurrentUICulture;

        var mainWindow = CreateAndConfigureMainWindow(serviceProvider);
        return mainWindow;
    }

    private static void RegisterRoutes(IRouteRegistry routes)
    {
        routes.Register(new RouteMap(Path: Routes.Home, ViewModel: typeof(ShellViewModel), Nested:
        [
            new RouteMap(Path: Routes.CreateOrganisation, ViewModel: typeof(Setup.CreateOrganisationViewModel)),
            new RouteMap(Path: Routes.CreateSuperUser, ViewModel: typeof(Setup.CreateSuperUserViewModel)),
            new RouteMap(Path: Routes.Login, ViewModel: typeof(Security.LoginScreenViewModel)),
            new RouteMap(Path: Routes.Dashboard, ViewModel: typeof(Shared.DashboardViewModel), Nested:
            [
                new RouteMap(Path: Routes.MembersOverview, ViewModel: typeof(Members.Pages.MembersOverviewViewModel), Nested:
                    [
                        new RouteMap(Path: Routes.MembersList, ViewModel: typeof(Members.Components.MemberListViewModel)),
                        new RouteMap(Path: Routes.MemberProfile, ViewModel: typeof(Members.Pages.MemberProfileViewModel)),
                    ]
                ),
                new RouteMap(Path: Routes.EventsOverview, ViewModel: typeof(EventsOverviewPageViewModel)),
                new RouteMap(Path: Routes.FinancesOverview, ViewModel: typeof(FinanceOverviewPageViewModel)),
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

    private static void UpdateDatabase(ILogger<Program> logger, IServiceProvider serviceProvider)
    {
        var dataContext = serviceProvider.GetService<LibotaDbContext>();
        if (dataContext == null) return;

        var pendingMigrations = dataContext.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length != 0)
        {
            LogApplyingPendingMigrations(logger, string.Join(", ", pendingMigrations));
            dataContext.Database.Migrate();
            LogTheDatabaseWasUpgraded(logger);
        }
        else
        {
            LogDatabaseIsUpToDate(logger);
        }
    }

    // Since LoggerMessage is partial, we'd need to put this in a partial class 
    // or just use logger.LogError directly for simplicity if it's not a performance critical path.
    // However, I will keep it simple for now or use the logger directly.
    private static void LogException(Microsoft.Extensions.Logging.ILogger logger) => logger.LogError("An error occurred during database update.");
    private static void LogApplyingPendingMigrations(Microsoft.Extensions.Logging.ILogger logger, string migrations) => logger.LogInformation("Applying pending migrations: [{migrations}]", migrations);
    private static void LogTheDatabaseWasUpgraded(Microsoft.Extensions.Logging.ILogger logger) => logger.LogInformation("The database was upgraded");
    private static void LogDatabaseIsUpToDate(Microsoft.Extensions.Logging.ILogger logger) => logger.LogInformation("Database is up-to-date!");
}
