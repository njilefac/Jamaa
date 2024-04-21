using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Akka.Hosting;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Libota.Data.Configuration;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Configuration;
using Libota.Desktop.Views.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactiveUI;

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

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifeTime) return;
            
            var serviceProvider = new ServiceCollection()
                .ConfigureAkka(lifeTime)
                .ConfigureServices()
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
            catch (Exception ex)
            {
                logger.LogError("{Exception}", ex);
            }
            
            Messages.Culture = CultureInfo.CurrentUICulture;
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            lifeTime.MainWindow = new MainWindow(serviceProvider)
            {
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
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