using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jamaa.Desktop.Services.Hosting;

/// <summary>
/// Initializes Elsa databases and ensures schema compatibility.
/// Handles schema creation and migration for both management and runtime databases.
/// </summary>
public static class ElsaDatabaseInitializer
{
    public static async Task InitializeAsync(
        WebApplication application,
        string managementConnectionString,
        string runtimeConnectionString,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("ElsaDatabaseInitializer: Starting database initialization");

        try
        {
            // Initialize management database
            logger.LogInformation("ElsaDatabaseInitializer: Initializing management database");
            await InitializeManagementDatabaseAsync(application, managementConnectionString, cancellationToken, logger);

            // Initialize runtime database
            logger.LogInformation("ElsaDatabaseInitializer: Initializing runtime database");
            await InitializeRuntimeDatabaseAsync(application, runtimeConnectionString, cancellationToken, logger);

            // Ensure schema compatibility
            logger.LogInformation("ElsaDatabaseInitializer: Ensuring schema compatibility");
            await EnsureSchemaCompatibilityAsync(managementConnectionString, cancellationToken);

            logger.LogInformation("ElsaDatabaseInitializer: Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ElsaDatabaseInitializer: Database initialization failed");
            throw;
        }
    }

    private static async Task InitializeManagementDatabaseAsync(
        WebApplication application,
        string connectionString,
        CancellationToken cancellationToken,
        ILogger logger)
    {
        try
        {
            await using var scope = application.Services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
            await using var context = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("ElsaDatabaseInitializer: Creating management database if not exists");
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("ElsaDatabaseInitializer: Management database ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ElsaDatabaseInitializer: Failed to initialize management database");
            throw;
        }
    }

    private static async Task InitializeRuntimeDatabaseAsync(
        WebApplication application,
        string connectionString,
        CancellationToken cancellationToken,
        ILogger logger)
    {
        try
        {
            await using var scope = application.Services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>();
            await using var context = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("ElsaDatabaseInitializer: Creating runtime database if not exists");
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("ElsaDatabaseInitializer: Runtime database ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ElsaDatabaseInitializer: Failed to initialize runtime database");
            throw;
        }
    }

    private static async Task EnsureSchemaCompatibilityAsync(
        string managementConnectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection(managementConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Check if WorkflowDefinitions table exists
            if (!await TableExistsAsync(connection, "WorkflowDefinitions", cancellationToken).ConfigureAwait(false))
                return; // Table doesn't exist yet, schema will be created by EnsureCreatedAsync

            // Ensure OriginalSource column exists
            if (!await ColumnExistsAsync(connection, "WorkflowDefinitions", "OriginalSource", cancellationToken).ConfigureAwait(false))
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "ALTER TABLE \"WorkflowDefinitions\" ADD COLUMN \"OriginalSource\" TEXT NULL;";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Log but don't fail - schema compatibility is a best-effort operation
            throw;
        }
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1;";
        command.Parameters.AddWithValue("$tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
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
}
