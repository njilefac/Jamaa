using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Domain.Values;
using Libota.Application.Configuration;
using Libota.Data.Configuration;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Configuration;
using Libota.Desktop.Views.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using Serilog.Settings.Configuration;

namespace Libota.Desktop
{
    public class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

            var services = ConfigureServices();

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                UpdateDatabase(logger, serviceProvider);
                var diagnosticListener = serviceProvider.GetService<IObserver<DiagnosticListener>>();
                if (diagnosticListener != null)
                    DiagnosticListener.AllListeners.Subscribe(diagnosticListener);

                
            }
            catch (Exception ex)
            {
                logger.LogError("{Exception}", ex);
            }
            
            Messages.Culture = CultureInfo.CurrentUICulture;
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            desktop.MainWindow = new MainWindow(serviceProvider)
            {
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
        }
        
        private static IServiceCollection ConfigureServices()
        {
            var environment = Environment.GetEnvironmentVariable("Environment") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appSettings.{environment}.json", false, true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            
            services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

            services
                .RegisterApplicationServices()
                .RegisterDataServices()
                .RegisterPresentationServices()
                .AddSerilog((_, l) =>
                {
                    l.ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
                    {
                        SectionName = "Serilog"
                    });
                });
            
            return services;
        }
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
}