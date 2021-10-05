using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;
using Domain.Values;
using EventFlow;
using EventFlow.Autofac.Extensions;
using Libota.Application.Configuration;
using Libota.Data.Configuration;
using Libota.Data.Notifiers;
using Libota.Desktop.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using Serilog.Extensions.Autofac.DependencyInjection;
using Splat;
using Splat.Autofac;

namespace Libota.Desktop
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = BuildAvaloniaApp(args);
            var logger = Locator.Current.GetService<ILogger<Program>>();
            try
            {
                UpdateDatabase(logger);
                var diagnosticListener = Locator.Current.GetService<IObserver<DiagnosticListener>>();
                if(diagnosticListener != null)
                    DiagnosticListener.AllListeners.Subscribe(diagnosticListener);
                
                app.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                logger.LogError("{Exception}", ex);
            }
        }

        private static void UpdateDatabase(ILogger<Program>? logger)
        {
            var dataContext = Locator.Current.GetService<LibotaDbContext>();
            if (dataContext == null) return;
            
            var pendingMigrations = dataContext.Database.GetPendingMigrations().ToArray();
            if (pendingMigrations.Any())
            {
                var migrationsList = string.Join(Environment.NewLine, pendingMigrations);
                logger.LogInformation($"applying pending migrations:{Environment.NewLine}{migrationsList}");
                dataContext.Database.Migrate();
                logger.LogInformation("the database was upgraded");
            }
            else
            {
                logger.LogInformation("database is up-to-date!");
            }
        }

        private static AppBuilder BuildAvaloniaApp(string[] args)
        {
            var appBuilder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseManagedSystemDialogs();

            ConfigureServices(args);


            return appBuilder;
        }

        private static void ConfigureServices(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("Environment") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appSettings.{environment}.json", false, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var services = new ServiceCollection();
            services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
            

            var containerBuilder = new ContainerBuilder();

            containerBuilder.Populate(services);

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration, "Serilog");

            containerBuilder
                .RegisterSerilog(loggerConfiguration)
                .RegisterModule<ApplicationServicesRegistration>()
                .RegisterModule<PresentationServicesRegistration>()
                .RegisterModule<DataServicesRegistration>();


            var resolver = containerBuilder.UseAutofacDependencyResolver();

            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(),
                typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));


            containerBuilder.RegisterInstance(resolver);
            resolver.InitializeReactiveUI();

            EventFlowOptions.New.UseAutofacContainerBuilder(containerBuilder)
                .RegisterModule<ApplicationEventingConfigurationModule>()
                .RegisterModule<DataEventingConfigurationModule>();

            var container = containerBuilder.Build();

            var mapperConfiguration = container.Resolve<MapperConfiguration>();
            mapperConfiguration.AssertConfigurationIsValid();
            resolver.SetLifetimeScope(container);
        }
    }
}