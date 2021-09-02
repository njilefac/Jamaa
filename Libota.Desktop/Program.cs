using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;
using Domain.Values;
using Libota.Application.Configuration;
using Libota.Data.Configuration;
using Libota.Desktop.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            app.StartWithClassicDesktopLifetime(args);
        }
        public static AppBuilder BuildAvaloniaApp(string[] args)
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
                .AddJsonFile($"app.{environment}.json", false, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var services = new ServiceCollection();
            services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
            services.AddObservableDataLayer();
            
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
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
            

            containerBuilder.RegisterInstance(resolver);
            resolver.InitializeReactiveUI();
            var container = containerBuilder.Build();

            var mapperConfiguration = container.Resolve<MapperConfiguration>();
            mapperConfiguration.AssertConfigurationIsValid();
            resolver.SetLifetimeScope(container);
        }
    }
}