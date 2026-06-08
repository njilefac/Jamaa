# Elsa Workflow Integration Setup

This document describes the Elsa Server + Studio WASM integration in the Jamaa solution.

## Architecture Overview

The solution follows the **Elsa Server + Studio (WASM)** pattern as documented in the [Elsa Workflows guide](https://docs.elsaworkflows.io/application-types/elsa-server-+-studio-wasm).

### Single ASP.NET Core Application

**Jamaa.Elsa.Server** is a single ASP.NET Core application that acts as **both**:
- **Elsa Server**: The workflow engine and API backend
- **Elsa Studio Host**: Serves the Blazor WebAssembly UI files

### Project Structure

```
Jamaa.sln
├── Jamaa.Elsa.Server/          ← Main host (ASP.NET Core)
│   ├── Program.cs              ← Elsa & Blazor configuration
│   ├── appsettings.json        ← Elsa HTTP API settings
│   ├── Pages/
│   │   └── _Host.cshtml        ← WASM host page
│   └── Jamaa.Elsa.Server.csproj ← References Studio project
│
└── Jamaa.Elsa.Studio/          ← Blazor WASM UI (compiled into Server)
    ├── Program.cs              ← Elsa Studio setup
    ├── wwwroot/
    │   └── appsettings.json    ← Studio auth & localization config
    ├── Layout/
    │   └── MainLayout.razor    ← Elsa shell layout
    └── Jamaa.Elsa.Studio.csproj
```

## How It Works

1. **Build Time**:
   - `Jamaa.Elsa.Server` includes a project reference to `Jamaa.Elsa.Studio`
   - When Server is built, Studio's Blazor WASM output is compiled to `wwwroot/`

2. **Runtime**:
   - Start: `dotnet run --project Jamaa.Elsa.Server`
   - Access: `https://localhost:5001`
   - The Server hosts both:
     - REST API at `/elsa/api/` (backend)
     - Studio UI at `/` (served as static WASM files)

3. **Communication**:
   - Studio (WASM frontend) makes HTTP calls to Server backend
   - Appears as two separate applications but deployed as one

## Configuration

### Server Configuration (appsettings.json)

```json
{
  "Http": {
    "BaseUrl": "https://localhost:5001",
    "BasePath": "/api/workflows"
  }
}
```

Configures HTTP activity settings for Elsa workflows.

### Studio Configuration (wwwroot/appsettings.json)

```json
{
  "Authentication": {
    "Provider": "ElsaIdentity"
  },
  "Localization": {
    "DefaultCulture": "en-US"
  }
}
```

- **Provider**: `"ElsaIdentity"` (built-in auth) or `"OpenIdConnect"` (external IdP)
- **Default Credentials** (dev): `admin` / `password`

## Running the Application

```bash
cd /Users/azamo/projects/Jamaa

# Build the solution
dotnet build

# Run the combined server
dotnet run --project Jamaa.Elsa.Server --urls https://localhost:5001
```

Then open browser to: **https://localhost:5001**

### First Login

- **Username**: `admin`
- **Password**: `password`

## Database

- **Type**: SQLite (file-based)
- **Location**: Default Elsa location (auto-created on first run)
- **Persistence**: Workflows, history, and scheduling data

## Future Integration with Avalonia

The Avalonia Desktop application can integrate with Elsa in two ways:

1. **Embedded Mode** (in a desktop WebView): Load the Studio UI within the Avalonia app
2. **HTTP Client Mode**: Use HTTP requests to communicate with the Elsa Server API

The REST API endpoints are available at:
- `/elsa/api/workflows` — Workflow management
- `/elsa/api/executions` — Workflow execution history
- etc.

## Technology Stack

- **Server**: ASP.NET Core 10.0
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Blazor WebAssembly (C#)
- **Expression Languages**: C#, JavaScript, Liquid
- **Authentication**: Elsa Identity (username/password)
- **UI Framework**: MudBlazor, Radzen

## References

- [Elsa Workflows Documentation](https://docs.elsaworkflows.io/)
- [Elsa Server + Studio Setup Guide](https://docs.elsaworkflows.io/application-types/elsa-server-+-studio-wasm)
