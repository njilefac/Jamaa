using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jamaa.Desktop.Configuration.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jamaa.Desktop.Services.Hosting;

public sealed partial class EmbeddedWebServer(
    ILogger<EmbeddedWebServer> logger,
    SqliteDatabaseConnection sqliteDatabaseConnection,
    IConfigurationRoot configuration) : IEmbeddedWebServer, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private WebApplication? _application;
    private Uri? _baseAddress;
    private readonly TaskCompletionSource<Uri> _startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Uri BaseAddress => _baseAddress ?? throw new InvalidOperationException("The embedded web server has not started.");

    public int Port => BaseAddress.Port;

    public Task<Uri> Started => _startedSource.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_application != null) return;

            logger.LogInformation("EmbeddedWebServer: Building Elsa connection strings");
            var managementConnStr = BuildElsaConnectionString(sqliteDatabaseConnection.Value, "elsa-management.db");
            var runtimeConnStr = BuildElsaConnectionString(sqliteDatabaseConnection.Value, "elsa-runtime.db");

            logger.LogInformation("EmbeddedWebServer: Building Elsa WebApplication");
            _application = ElsaWebApplicationBuilder.BuildElsaWebApplication(logger, managementConnStr, runtimeConnStr, configuration);

            logger.LogInformation("EmbeddedWebServer: Starting WebApplication");
            await _application.StartAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("EmbeddedWebServer: Initializing Elsa databases");
            await ElsaDatabaseInitializer.InitializeAsync(_application, managementConnStr, runtimeConnStr, logger, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("EmbeddedWebServer: Resolving base address");
            _baseAddress = ResolveBaseAddress(_application);
            _startedSource.TrySetResult(_baseAddress);
            LogEmbeddedServerStarted(_baseAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmbeddedWebServer: Startup failed");
            _startedSource.TrySetException(ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_application == null) return;

            logger.LogInformation("EmbeddedWebServer: Stopping");
            await _application.StopAsync(cancellationToken).ConfigureAwait(false);
            await _application.DisposeAsync().ConfigureAwait(false);
            _application = null;
            logger.LogInformation("EmbeddedWebServer: Stopped");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _gate.Dispose();
    }

    private static string BuildElsaConnectionString(string jamaaConnectionString, string fileName)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(jamaaConnectionString);
        var directory = Path.GetDirectoryName(builder.DataSource) ?? AppContext.BaseDirectory;
        builder.DataSource = Path.Combine(directory, fileName);
        return builder.ConnectionString;
    }

    private static Uri ResolveBaseAddress(WebApplication application)
    {
        var addresses = application.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses;

        var address = addresses?.FirstOrDefault()
                      ?? application.Urls.FirstOrDefault()
                      ?? throw new InvalidOperationException("The embedded web server did not expose a listening address.");

        return new Uri(address);
    }
}
