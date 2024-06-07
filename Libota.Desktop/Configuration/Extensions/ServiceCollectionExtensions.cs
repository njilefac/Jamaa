using System;
using System.IO;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Persistence.Sql.Hosting;
using Avalonia.Controls.ApplicationLifetimes;
using Domain.Shared.Values;
using Libota.Application.Configuration;
using Libota.Application.Shared;
using Libota.Data.Configuration;
using Libota.Desktop.Configuration.Hosting;
using LinqToDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Settings.Configuration;

namespace Libota.Desktop.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static ServiceCollection ConfigureAkka(this ServiceCollection services, IClassicDesktopStyleApplicationLifetime applicationLifetime, IConfigurationRoot configuration)
    {
        services.AddSingleton<IHostApplicationLifetime>(new AvaloniaApplicationLifeTime(applicationLifetime));

        services.AddAkka("Libota-Akka", builder =>
        {
            builder.WithActors((system, registry, resolver) =>
            {
                var commandProcessor = system.ActorOf(resolver.Props<CommandProcessor>(), "command-processor");

                var organisationEventsProjector = system.ActorOf(resolver.Props<OrganisationProjection>(), "organisation-events-projector");


                registry.Register<CommandProcessor>(commandProcessor);
                registry.Register<OrganisationProjection>(organisationEventsProjector);

                system.WhenTerminated.ContinueWith(_ => applicationLifetime.Shutdown());
            });

            builder.ConfigureLoggers(b =>
            {
                b.ClearLoggers();
                b.AddLogger<SerilogLogger>();
            });

            var connectionString = $"Data Source={Path.Combine(Directory.GetCurrentDirectory(),
                configuration.GetSection("Database:DataFile").Value ?? throw new InvalidOperationException())};";

            
            
            builder.WithSqlPersistence(connectionString,
                ProviderName.SQLiteMS,
                journalBuilder:b =>
                {
                    b.AddWriteEventAdapter<LibotaEventTagger>("organisation-event-tagger", new[] { typeof(ILibotaEvent) });
                },
                autoInitialize: true,
                useWriterUuidColumn: true);
        });

        return services;
    }

    public static ServiceCollection ConfigureServices(this ServiceCollection services, IConfigurationRoot configuration)
    {
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