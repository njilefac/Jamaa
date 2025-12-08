using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Libota.Application.Shared.Logging;
using Libota.Data.Configuration;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Configuration.Extensions;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.Views.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Libota.Desktop;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifeTime) return;

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

        ServiceProvider = new ServiceCollection()
            .ConfigureServices(configuration)
            .ConfigureAkka(lifeTime, configuration)
            .BuildServiceProvider();

        var akkaService = ServiceProvider.GetRequiredService<IHostedService>();
        await akkaService.StartAsync(CancellationToken.None);

        var logger = ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            UpdateDatabase(logger, ServiceProvider);
            var diagnosticListener = ServiceProvider.GetService<IObserver<DiagnosticListener>>();
            if (diagnosticListener != null)
                DiagnosticListener.AllListeners.Subscribe(diagnosticListener);
        }
        catch (Exception ex)
        {
            logger.LogError("{Exception}", ex);
        }

        Messages.Culture = CultureInfo.CurrentUICulture;
        if (ServiceProvider.GetService<IViewFor<MainWindowViewModel>>() is not MainWindow mainWindow) return;
        var mainViewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainViewModel;
        lifeTime.MainWindow = mainWindow;
    }

    private ServiceProvider ServiceProvider { get; set; }


    private static void UpdateDatabase(ILogger<Program>? logger, IServiceProvider serviceProvider)
    {
        var dataContext = serviceProvider.GetService<LibotaDbContext>();
        if (dataContext == null) return;

        var pendingMigrations = dataContext.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length != 0)
        {
            logger?.LogInformation("applying pending migrations: [{Migrations}]", string.Join(", ", pendingMigrations));
            dataContext.Database.Migrate();
            logger?.LogInformation("the database was upgraded");
        }
        else
        {
            logger?.LogInformation("database is up-to-date!");
        }
    }
}