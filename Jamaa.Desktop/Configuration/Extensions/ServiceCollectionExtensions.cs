using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Persistence.Sql.Hosting;
using Avalonia.Controls.ApplicationLifetimes;
using Domain.Shared.Values;
using Jamaa.Application.Configuration;
using Jamaa.Application.Shared;
using Jamaa.Data.Configuration;
using Jamaa.Desktop.Configuration.Hosting;
using LinqToDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Settings.Configuration;

namespace Jamaa.Desktop.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    extension(ServiceCollection services)
    {
        public ServiceCollection ConfigureAkka(IClassicDesktopStyleApplicationLifetime applicationLifetime,
            IConfigurationRoot configuration)
        {
            services.AddSingleton<IHostApplicationLifetime>(new AvaloniaApplicationLifeTime(applicationLifetime));

            services.AddAkka("Libota-Akka", builder =>
            {
                builder.WithActors((system, registry, resolver) =>
                {
                    var commandProcessor = system.ActorOf(resolver.Props<CommandProcessor>(), "command-processor");

                    var organisationEventsProjector = system.ActorOf(resolver.Props<OrganisationProjection>(),
                        "organisation-events-projector");


                    registry.Register<CommandProcessor>(commandProcessor);
                    registry.Register<OrganisationProjection>(organisationEventsProjector);

                    system.WhenTerminated.ContinueWith(_ =>
                    {
                        try
                        {
                            applicationLifetime.Shutdown();
                        }
                        catch (TaskCanceledException)
                        {
                            // Ignore, the application is already shutting down
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error during application shutdown");
                        }
                    });
                });

                builder.ConfigureLoggers(b =>
                {
                    b.LogLevel = Akka.Event.LogLevel.WarningLevel;
                    b.ClearLoggers();
                    b.AddLogger<SerilogLogger>();
                });

                var connectionString = $"Data Source={ResolveDataPath(configuration) ?? throw new InvalidOperationException()};";


                builder.WithSqlPersistence(connectionString,
                    ProviderName.SQLiteMS,
                    journalBuilder: b =>
                    {
                        b.AddWriteEventAdapter<LibotaEventTagger>("organisation-event-tagger", [typeof(ILibotaEvent)]);
                    },
                    autoInitialize: true,
                    useWriterUuidColumn: true);
            });

            return services;
        }

        public ServiceCollection ConfigureServices(IConfigurationRoot configuration)
        {
            services.AddLogging();

            services.Configure<DatabaseOptions>(options =>
            {
                configuration.GetSection("Database").Bind(options);
                options.DataFile = ResolveDataPath(configuration);
            });

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

    private static string ResolveDataPath(IConfigurationRoot configurationRoot)
    {
        var baseAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var jamaaDataFolder = Path.Combine(baseAppData, "Jamaa");

        if (!Directory.Exists(jamaaDataFolder))
        {
            Directory.CreateDirectory(jamaaDataFolder);
        }

        var dbFileName = configurationRoot.GetSection("Database:DataFile").Value
                         ?? throw new InvalidOperationException("Database filename missing in config.");

        var dbPath = Path.Combine(jamaaDataFolder, dbFileName);
        return dbPath;
    }
}