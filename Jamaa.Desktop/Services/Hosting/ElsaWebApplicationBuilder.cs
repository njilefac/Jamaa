using System;
using System.Net;
using System.Threading.Tasks;
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
        builder.WebHost.UseStaticWebAssets();

        // Configure logging (reuse the app-wide Serilog instance and sinks).
        logger.LogInformation("ElsaWebApplicationBuilder: Configuring Serilog");
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: false);

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
        // Static file serving (for Elsa Studio Blazor WASM assets and other static files)
        // Map generated static web assets (_framework, _content, scoped CSS, etc.)
        // and serve any additional file-based assets from the host's web root.
        app.MapStaticAssets();
        app.UseRouting();
        app.UseStaticFiles();
        app.UseCors();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Elsa APIs
        app.UseWorkflowsApi();
        app.UseWorkflows();

        // Blazor WASM fallback for client-side routing
        app.MapFallback(ServeElsaStudioHostPageAsync);

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));
    }

    private static Task ServeElsaStudioHostPageAsync(HttpContext context)
    {
        var basePath = context.Request.PathBase.Value ?? string.Empty;
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var apiUrl = baseUrl + basePath + "/elsa/api";
        var html = $$"""
                    <!DOCTYPE html>
                    <html>

                    <head>
                        <meta charset="utf-8"/>
                        <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"/>
                        <title>Elsa Studio - Embedded</title>
                        <base href="/"/>
                        <link rel="apple-touch-icon" sizes="180x180" href="{{basePath}}/_content/Elsa.Studio.Shell/apple-touch-icon.png">
                        <link rel="icon" type="image/png" sizes="32x32" href="{{basePath}}/_content/Elsa.Studio.Shell/favicon-32x32.png">
                        <link rel="icon" type="image/png" sizes="16x16" href="{{basePath}}/_content/Elsa.Studio.Shell/favicon-16x16.png">
                        <link rel="manifest" href="{{basePath}}/_content/Elsa.Studio.Shell/site.webmanifest">
                        <link rel="preconnect" href="https://fonts.googleapis.com">
                        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                        <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet"/>
                        <link href="https://fonts.googleapis.com/css2?family=Ubuntu:wght@300;400;500;700&display=swap" rel="stylesheet">
                        <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap" rel="stylesheet">
                        <link href="https://fonts.googleapis.com/css2?family=Grandstander:wght@100&display=swap" rel="stylesheet">
                        <link href="{{basePath}}/_content/MudBlazor/MudBlazor.min.css" rel="stylesheet"/>
                        <link href="{{basePath}}/_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet"/>
                        <link href="{{basePath}}/_content/Radzen.Blazor/css/material-base.css" rel="stylesheet">
                        <link href="{{basePath}}/_content/Elsa.Studio.Shell/css/shell.css" rel="stylesheet">
                        <link href="Jamaa.Elsa.Studio.styles.css" rel="stylesheet">
                    </head>

                    <body>
                    <div id="app">
                        <div class="loading-splash mud-container mud-container-maxwidth-false">
                            <h5 class="mud-typography mud-typography-h5 mud-primary-text my-6">Loading Elsa Studio...</h5>
                        </div>
                    </div>

                    <div id="blazor-error-ui">
                        An unhandled error has occurred.
                        <a href="" class="reload">Reload</a>
                        <a class="dismiss">🗙</a>
                    </div>
                    <script src="{{basePath}}/_content/BlazorMonaco/jsInterop.js"></script>
                    <script src="{{basePath}}/_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>
                    <script src="{{basePath}}/_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>
                    <script src="{{basePath}}/_content/MudBlazor/MudBlazor.min.js"></script>
                    <script src="{{basePath}}/_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>
                    <script src="{{basePath}}/_content/Radzen.Blazor/Radzen.Blazor.js"></script>
                    <script>
                        window.getClientConfig = function() { return {
                            "apiUrl": "{{apiUrl}}",
                            "basePath": "{{basePath}}"
                         } };
                    </script>
                    <script src="_framework/blazor.webassembly.js"></script>
                    </body>

                    </html>
                    """;

        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(html);
    }
}
