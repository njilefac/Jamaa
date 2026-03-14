using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using CreateOrganisationViewModel = Libota.Desktop.Setup.CreateOrganisationViewModel;
using CreateSuperUserViewModel = Libota.Desktop.Setup.CreateSuperUserViewModel;
using DashboardViewModel = Libota.Desktop.Shared.DashboardViewModel;
using LoginScreenViewModel = Libota.Desktop.Security.LoginScreenViewModel;
using MemberListViewModel = Libota.Desktop.Members.Components.MemberListViewModel;
using MemberProfileViewModel = Libota.Desktop.Members.Pages.MemberProfileViewModel;
using MembersOverviewViewModel = Libota.Desktop.Members.Pages.MembersOverviewViewModel;
using Shell = Libota.Desktop.Shared.Shell;
using ShellViewModel = Libota.Desktop.Shared.ShellViewModel;

namespace Libota.Desktop;

public partial class Program
{
    public static void Main(string[] args)
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .AfterSetup(builder =>
            {
                var lifeTime = builder.Instance?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
                               ?? throw new InvalidOperationException();

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
                akkaService.StartAsync(CancellationToken.None);

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
                lifeTime.MainWindow = mainWindow;
            })
            .StartWithClassicDesktopLifetime(args);
    }

    private static void RegisterRoutes(IRouteRegistry routes)
    {
        routes.Register(new RouteMap(Path: Routes.Home, ViewModel: typeof(ShellViewModel), Nested:
        [
            new RouteMap(Path: Routes.CreateOrganisation, ViewModel: typeof(CreateOrganisationViewModel)),
            new RouteMap(Path: Routes.CreateSuperUser, ViewModel: typeof(CreateSuperUserViewModel)),
            new RouteMap(Path: Routes.Login, ViewModel: typeof(LoginScreenViewModel)),
            new RouteMap(Path: Routes.Dashboard, ViewModel: typeof(DashboardViewModel), Nested:
            [
                new RouteMap(Path: Routes.MembersOverview, ViewModel: typeof(MembersOverviewViewModel), Nested:
                    [
                        new RouteMap(Path: Routes.MembersList, ViewModel: typeof(MemberListViewModel)),
                        new RouteMap(Path: Routes.MemberProfile, ViewModel: typeof(MemberProfileViewModel)),
                    ]
                ),
                new RouteMap(Path: Routes.EventsOverview, ViewModel: typeof(EventsOverviewPageViewModel)),
                new RouteMap(Path: Routes.FinancesOverview, ViewModel: typeof(FinanceOverviewPageViewModel)),
            ]),
        ]));
    }

    private static Shell CreateAndConfigureMainWindow(ServiceProvider serviceProvider)
    {
        var mainWindow = new Shell();
        var mainViewModel = (serviceProvider ?? throw new InvalidOperationException())
            .GetRequiredService<ShellViewModel>();
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

    [LoggerMessage(LogLevel.Error)]
    static partial void LogException(ILogger<Program> logger);

    [LoggerMessage(LogLevel.Information, "applying pending migrations: [{migrations}]")]
    static partial void LogApplyingPendingMigrations(ILogger<Program> logger, string migrations);

    [LoggerMessage(LogLevel.Information, "the database was upgraded")]
    static partial void LogTheDatabaseWasUpgraded(ILogger<Program> logger);

    [LoggerMessage(LogLevel.Information, "database is up-to-date!")]
    static partial void LogDatabaseIsUpToDate(ILogger<Program> logger);
}