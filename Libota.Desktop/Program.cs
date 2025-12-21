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
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.Views.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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

    private static MainWindow CreateAndConfigureMainWindow(ServiceProvider serviceProvider)
    {
        var mainWindow = new MainWindow();
        var mainViewModel = (serviceProvider ?? throw new InvalidOperationException())
            .GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainViewModel;
        return mainWindow;
    }

    private static void UpdateDatabase(ILogger<Program>? logger, IServiceProvider serviceProvider)
    {
        var dataContext = serviceProvider.GetService<LibotaDbContext>();
        if (dataContext == null) return;

        var pendingMigrations = dataContext.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length != 0)
        {
            LogApplyingPendingMigrationsMigrations(logger, string.Join(", ", pendingMigrations));
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
    static partial void LogApplyingPendingMigrationsMigrations(ILogger<Program> logger, string migrations);

    [LoggerMessage(LogLevel.Information, "the database was upgraded")]
    static partial void LogTheDatabaseWasUpgraded(ILogger<Program> logger);

    [LoggerMessage(LogLevel.Information, "database is up-to-date!")]
    static partial void LogDatabaseIsUpToDate(ILogger<Program> logger);
}