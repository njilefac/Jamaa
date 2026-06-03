using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jamaa.Desktop.Configuration.Hosting;
using Elsa.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Tenants.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Settings.Configuration;

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

            var elsaManagementConnectionString = BuildElsaConnectionString(sqliteDatabaseConnection.Value, "elsa-management.db");
            var elsaRuntimeConnectionString = BuildElsaConnectionString(sqliteDatabaseConnection.Value, "elsa-runtime.db");
            _application = BuildApplication(elsaManagementConnectionString, elsaRuntimeConnectionString);

            await EnsureElsaDatabasesReadyAsync(cancellationToken).ConfigureAwait(false);
            await EnsureElsaSchemaCompatibilityAsync(elsaManagementConnectionString, cancellationToken).ConfigureAwait(false);

            await _application.StartAsync(cancellationToken).ConfigureAwait(false);
            _baseAddress = ResolveBaseAddress(_application);
            _startedSource.TrySetResult(_baseAddress);
            LogEmbeddedServerStarted(_baseAddress);
        }
        catch (Exception ex)
        {
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

            await _application.StopAsync(cancellationToken).ConfigureAwait(false);
            await _application.DisposeAsync().ConfigureAwait(false);
            _application = null;
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

    private WebApplication BuildApplication(string elsaManagementConnectionString, string elsaRuntimeConnectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory
        });
        var embeddedLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
            {
                SectionName = "Serilog"
            })
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(embeddedLogger, dispose: true);

        var elsaSqliteOptions = new ElsaDbContextOptions { SchemaName = null };

        builder.Services.AddElsa(elsa =>
        {
            elsa.UseWorkflowManagement(management =>
                management.UseEntityFrameworkCore(ef =>
                {
                    ef.RunMigrations = false;
                    ef.UseSqlite(elsaManagementConnectionString, elsaSqliteOptions);
                }));

            elsa.UseWorkflowRuntime(runtime =>
                runtime.UseEntityFrameworkCore(ef =>
                {
                    ef.RunMigrations = false;
                    ef.UseSqlite(elsaRuntimeConnectionString, elsaSqliteOptions);
                }));

            elsa.UseWorkflowsApi();

            elsa.UseScheduling();
            elsa.UseHttp();
            elsa.UseTenants();
            elsa.UseTenantHttpRouting(f => f.WithTenantHeader("x-tenant"));
        });

        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        builder.Services.AddRazorPages();

        var application = builder.Build();
        application.MapGet("/", () => Results.Text("Jamaa embedded server"));
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        return application;
    }

    /// <summary>
    /// Calls EnsureCreatedAsync on each Elsa DbContext. On a fresh elsa.db this creates
    /// the complete schema from the current entity model (Elsa 3.7.0), including all
    /// columns that the 3.5.3 EF migrations never added. On an existing file it is a
    /// fast no-op (returns false without touching the schema).
    /// </summary>
    private async Task EnsureElsaDatabasesReadyAsync(CancellationToken cancellationToken)
    {
        if (_application == null) return;

        await using var managementScope = _application.Services.CreateAsyncScope();
        var managementFactory = managementScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
        await using var managementContext = await managementFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await managementContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var runtimeScope = _application.Services.CreateAsyncScope();
        var runtimeFactory = runtimeScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>();
        await using var runtimeContext = await runtimeFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await runtimeContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureElsaSchemaCompatibilityAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await TableExistsAsync(connection, "WorkflowDefinitions", cancellationToken).ConfigureAwait(false))
            return;

        if (await ColumnExistsAsync(connection, "WorkflowDefinitions", "OriginalSource", cancellationToken).ConfigureAwait(false))
            return;

        await using var addColumnCommand = connection.CreateCommand();
        addColumnCommand.CommandText = "ALTER TABLE \"WorkflowDefinitions\" ADD COLUMN \"OriginalSource\" TEXT NULL;";
        await addColumnCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1;";
        command.Parameters.AddWithValue("$tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var currentColumn = reader.GetString(reader.GetOrdinal("name"));
            if (string.Equals(currentColumn, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string BuildElsaConnectionString(string jamaaConnectionString, string fileName)
    {
        var builder = new SqliteConnectionStringBuilder(jamaaConnectionString);
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
