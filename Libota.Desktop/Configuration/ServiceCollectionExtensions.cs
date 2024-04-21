using System;
using System.IO;
using Akka.Hosting;
using Akka.Persistence.Sqlite;
using Avalonia.Controls.ApplicationLifetimes;
using Domain.Values;
using Libota.Application.Configuration;
using Libota.Application.Shared;
using Libota.Data.Configuration;
using Libota.Desktop.Configuration.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Settings.Configuration;

namespace Libota.Desktop.Configuration;

public static class ServiceCollectionExtensions
{
    public static ServiceCollection ConfigureAkka(this ServiceCollection services, IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        services.AddSingleton<IHostApplicationLifetime>(new AvaloniaApplicationLifeTime(applicationLifetime));
        
        services.AddAkka("Libota-Akka", builder =>
        {
            builder.WithActors((system, registry, resolver) =>
            {
                var commandProcessor = system.ActorOf(resolver.Props<CommandProcessor>(), "command-processor");
                var queryProcessor = system.ActorOf(resolver.Props<QueryProcessor>(), "query-processor");

                registry.Register<CommandProcessor>(commandProcessor);
                registry.Register<QueryProcessor>(queryProcessor);

                SqlitePersistence.Get(system);

                system.WhenTerminated.ContinueWith(_ => applicationLifetime.Shutdown());
            });
        });
            
        return services;
    }
    
    public static ServiceCollection ConfigureServices(this ServiceCollection services)
    {
        var environment = Environment.GetEnvironmentVariable("Environment") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appSettings.{environment}.json", false, true)
            .AddEnvironmentVariables()
            .Build();

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
}