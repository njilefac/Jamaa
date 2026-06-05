using System;
using System.Net;
using Elsa.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Tenants.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Settings.Configuration;

namespace Jamaa.Desktop.Services.Hosting;

/// <summary>
/// Builds a WebApplication configured with Elsa Workflows + Studio.
/// Encapsulates all Elsa setup logic for clean separation of concerns.
/// </summary>
public static class ElsaWebApplicationBuilder
{
    public static WebApplication BuildElsaWebApplication(
        Microsoft.Extensions.Logging.ILogger logger,
        string elsaManagementConnectionString,
        string elsaRuntimeConnectionString,
        IConfigurationRoot configuration)
    {
        logger.LogInformation("ElsaWebApplicationBuilder: Creating WebApplicationBuilder");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory
        });

        // Configure logging
        logger.LogInformation("ElsaWebApplicationBuilder: Configuring Serilog");
        var embeddedLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
            {
                SectionName = "Serilog"
            })
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(embeddedLogger, dispose: true);

        // Configure Elsa services
        logger.LogInformation("ElsaWebApplicationBuilder: Adding Elsa services");
        ConfigureElsaServices(builder.Services, elsaManagementConnectionString, elsaRuntimeConnectionString);

        // Configure Kestrel to listen on loopback with dynamic port
        logger.LogInformation("ElsaWebApplicationBuilder: Configuring Kestrel");
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        // Build the application
        logger.LogInformation("ElsaWebApplicationBuilder: Building WebApplication");
        var application = builder.Build();

        // Configure middleware pipeline
        logger.LogInformation("ElsaWebApplicationBuilder: Configuring middleware pipeline");
        ConfigureMiddleware(application);

        logger.LogInformation("ElsaWebApplicationBuilder: WebApplication built successfully");
        return application;
    }

    private static void ConfigureElsaServices(IServiceCollection services, string managementConnectionString, string runtimeConnectionString)
    {
        var elsaSqliteOptions = new ElsaDbContextOptions { SchemaName = null };

        services.AddElsa(elsa =>
        {
            // Identity & Authentication
            elsa.UseIdentity(identity =>
            {
                identity.TokenOptions = options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens";
                identity.UseAdminUserProvider();
            })
            .UseDefaultAuthentication()

            // Workflow Management (stores workflow definitions)
            .UseWorkflowManagement(management =>
                management.UseEntityFrameworkCore(ef =>
                {
                    ef.RunMigrations = false; // Let EnsureCreatedAsync handle schema
                    ef.UseSqlite(managementConnectionString, elsaSqliteOptions);
                }))

            // Workflow Runtime (stores execution state)
            .UseWorkflowRuntime(runtime =>
                runtime.UseEntityFrameworkCore(ef =>
                {
                    ef.RunMigrations = false; // Let EnsureCreatedAsync handle schema
                    ef.UseSqlite(runtimeConnectionString, elsaSqliteOptions);
                }))

            // APIs and features
            .UseWorkflowsApi()
            .UseScheduling()
            .UseHttp()
            .UseTenants()
            .UseTenantHttpRouting(f => f.WithTenantHeader("x-tenant"))
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>();
        });

        // CORS for Blazor WASM frontend
        services.AddCors(cors => cors.AddDefaultPolicy(policy =>
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
                .WithExposedHeaders("*")));

        // Razor Pages for Blazor WASM hosting
        services.AddRazorPages(options =>
            options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        // Static assets and routing
        app.MapStaticAssets();
        app.UseRouting();
        app.UseCors();
        app.UseStaticFiles();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Elsa APIs
        app.UseWorkflowsApi();
        app.UseWorkflows();

        // Blazor WASM fallback for client-side routing
        app.MapFallbackToPage("/");

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));
    }
}
