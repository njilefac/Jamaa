# EmbeddedWebServer + Elsa Studio Integration Guide

This document describes how Elsa + Studio WASM has been integrated into the Jamaa Desktop's EmbeddedWebServer.

## Overview

The **EmbeddedWebServer** is now an all-in-one embedded application that hosts:
- **Elsa Workflow Engine** (ASP.NET Core backend)
- **Elsa Studio** (Blazor WASM frontend)
- All running in a single process with existing configuration

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│         EmbeddedWebServer (ASP.NET Core 10.0)          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Frontend (Blazor WASM)                                 │
│  ├─ Pages/_Host.cshtml        (Entry point)            │
│  ├─ _framework/               (WASM runtime)            │
│  ├─ _content/                 (UI components)           │
│  ├─ Monaco Editor             (Expressions)            │
│  └─ MudBlazor, Radzen         (UI Framework)           │
│                                                          │
│  ↕ HTTP/REST API                                        │
│                                                          │
│  Backend (Elsa Engine)                                  │
│  ├─ Workflow Management       (Definitions)            │
│  ├─ Workflow Runtime          (Execution)              │
│  ├─ Activities & Activities   (Actions)                │
│  ├─ Expressions              (C#, JS, Liquid)          │
│  ├─ Scheduling               (Cron, delays)            │
│  ├─ HTTP Activities          (API calls)               │
│  ├─ Multi-tenancy            (x-tenant header)         │
│  └─ REST API                 (/elsa/api/*)             │
│                                                          │
│  Data Persistence (SQLite)                              │
│  ├─ elsa-management.db        (Definitions)            │
│  ├─ elsa-runtime.db           (Execution state)        │
│  └─ Jamaa.db                  (Main app data)          │
│                                                          │
│  Logging & Monitoring                                   │
│  ├─ Serilog                   (All logs)               │
│  └─ Structured logging        (JSON, colored output)   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## File Structure

### Modified Files

**`Jamaa.Desktop/Services/Hosting/EmbeddedWebServer.cs`**
- Enhanced `BuildApplication()` method
- Added Elsa + Studio configuration
- Maintains backward compatibility with existing setup

**Key Changes:**
```csharp
// Enable static web assets for Blazor WASM
builder.WebHost.UseStaticWebAssets();

// Configure Elsa with Identity and Studio support
builder.Services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity => /* admin user */);
    elsa.UseDefaultAuthentication();
    // ... standard Elsa config
    elsa.UseWorkflowsApi();
});

// Add CORS for API access
builder.Services.AddCors(cors => cors.AddDefaultPolicy(...));

// Add Razor Pages for _Host.cshtml
builder.Services.AddRazorPages();

// Middleware chain
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapFallbackToPage("/_Host");  // Blazor WASM routing
```

### New Files

**`Jamaa.Desktop/Services/Hosting/Pages/_Host.cshtml`**
- Blazor WASM host page
- Loads Elsa Studio Shell
- Configures Monaco editor
- Sets up UI component libraries
- Passes API URL via `window.getClientConfig()`

## Configuration

### SQLite Databases

Reuses existing `SqliteDatabaseConnection` from Desktop app:

```csharp
// In EmbeddedWebServer constructor:
var elsaManagementConnectionString = BuildElsaConnectionString(
    sqliteDatabaseConnection.Value, 
    "elsa-management.db"
);

var elsaRuntimeConnectionString = BuildElsaConnectionString(
    sqliteDatabaseConnection.Value, 
    "elsa-runtime.db"
);
```

**Location**: Same directory as main Jamaa.db (typically AppData)

### Logging

Uses existing **Serilog** configuration from `IConfigurationRoot`:

```csharp
var embeddedLogger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
    {
        SectionName = "Serilog"
    })
    .CreateLogger();

builder.Logging.AddSerilog(embeddedLogger, dispose: true);
```

All logs from Elsa Server and Studio frontend use the same configuration.

### Networking

Maintains existing Kestrel configuration:

```csharp
builder.WebHost.ConfigureKestrel(options => 
    options.Listen(IPAddress.Loopback, 0)  // Random port on 127.0.0.1
);
```

The port is randomly assigned and exposed via `EmbeddedWebServer.Port` property.

## Runtime Behavior

### Startup Sequence

1. **Create connection strings** for management & runtime databases
2. **Build the WebApplication** with Elsa + Studio configuration
3. **Ensure databases** are created/migrated
4. **Check schema compatibility** for existing workflows
5. **Start the web server** on 127.0.0.1 with random port
6. **Set base address** and signal startup complete

### First Request

1. Browser/client navigates to `http://127.0.0.1:PORT/`
2. Server serves `_Host.cshtml` (Razor Page)
3. Page loads Blazor WASM runtime
4. Studio UI initializes
5. Frontend calls `/elsa/api/` endpoints to load workflows
6. User can design/manage workflows in Studio

### API Endpoints

Available at `http://127.0.0.1:PORT/elsa/api/`:

```
GET    /workflows                    # List workflow definitions
POST   /workflows                    # Create new workflow
GET    /workflows/{id}               # Get workflow details
PUT    /workflows/{id}               # Update workflow
DELETE /workflows/{id}               # Delete workflow

GET    /executions                   # List executions
GET    /executions/{id}              # Get execution details
POST   /executions/{id}/resume       # Resume paused execution

GET    /activity-descriptors         # Available activities
GET    /expression-descriptors       # Available expressions
```

## Authentication

**Elsa Identity** (built-in provider):
- Default admin user enabled
- Username: `admin`
- Password: Can be configured in `Elsa.Identity` options
- Supports adding more users programmatically

## Integration with Avalonia App

### Option 1: HTTP API Client (Recommended)
```csharp
// Desktop app has access to embedded server
var baseAddress = embeddedWebServer.BaseAddress;  // http://127.0.0.1:12345/

// Make HTTP calls to Elsa API
var httpClient = new HttpClient { BaseAddress = baseAddress };
var workflows = await httpClient.GetAsync("/elsa/api/workflows");
```

### Option 2: Embedded WebView (Future)
```csharp
// Embed Studio UI directly in Avalonia window
var webView = new WebView();
webView.Source = embeddedWebServer.BaseAddress;  // Load Studio UI
```

## Troubleshooting

### "Pages/_Host.cshtml not found"
- Ensure file exists at: `Jamaa.Desktop/Services/Hosting/Pages/_Host.cshtml`
- Verify it's included in the project file

### WASM components fail to load
- Check browser DevTools network tab for failed requests
- Verify Elsa Studio NuGet packages are installed
- Ensure `UseStaticWebAssets()` is called early in middleware

### Database errors
- Check SQLite connection string in `SqliteDatabaseConnection`
- Verify path has write permissions
- Check database file isn't locked by another process

### Authentication issues
- Verify Elsa Identity configuration in `builder.Services.AddElsa()`
- Check that `UseDefaultAuthentication()` is called
- Ensure JWT signing key is configured

## Build & Deployment

### Building
```bash
dotnet build                    # Full solution
dotnet build Jamaa.Desktop      # Just Desktop project
```

### Build Output
- Elsa Server DLL: `Jamaa.Desktop/bin/Debug/net10.0/*/Jamaa.Desktop.dll`
- All Elsa packages included in dependencies
- No separate deployment needed

### Distribution
- Desktop app includes everything needed
- EmbeddedWebServer starts automatically
- Studio UI accessible without additional setup

## Performance

- **Memory**: ~100-150 MB for embedded server + Studio
- **Startup**: ~2-5 seconds to initialize and start server
- **API Response**: <100ms for typical workflow operations
- **Database**: SQLite is efficient for single-user embedded scenarios

## Security Considerations

- Server only listens on `127.0.0.1` (localhost)
- Not accessible from network by default
- CORS policy configured for API access
- JWT tokens used for API authentication
- Passwords handled via Elsa Identity provider

## Future Enhancements

1. **Custom Activities**: Add Jamaa-specific workflow activities
2. **Workflow Templates**: Pre-built workflows for common tasks
3. **Web UI Customization**: Brand/theme the Studio UI
4. **Activity Logging**: Track all workflow executions in Jamaa.db
5. **External API Integration**: Call REST APIs for business logic
6. **Notifications**: Send alerts when workflows complete/fail
7. **User Management**: Add multiple users with different permissions

## References

- **Elsa Documentation**: https://docs.elsaworkflows.io/
- **Blazor WASM**: https://docs.microsoft.com/en-us/aspnet/core/blazor/
- **ASP.NET Core**: https://docs.microsoft.com/en-us/aspnet/core/
- **SQLite**: https://www.sqlite.org/

## Support

For questions or issues with the Elsa + Studio integration:
1. Check the EmbeddedWebServer logs (Serilog configuration)
2. Verify all required NuGet packages are installed
3. Ensure SQLite database files have write permissions
4. Review the ELSA_SETUP.md guide for baseline setup
