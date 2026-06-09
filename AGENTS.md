# AGENTS.md — Jamaa Development Guide for AI Assistants

This guide helps AI coding agents understand the Jamaa codebase architecture, conventions, and workflows. Last updated: June 2026.

---

## Quick Facts

- **Tech Stack**: .NET 10, C# 14, Avalonia UI (cross-platform desktop), Akka.NET (actors), Entity Framework Core
- **Architecture**: Clean Architecture + Domain-Driven Design (DDD)
- **Build Target**: macOS first (packaged via PowerShell scripts in `/bundle`), cross-platform UI
- **Key Pattern**: Integration/Operation split (IOSP) for all methods; Command-Query separation via actors
- **Workflow Tooling**: Elsa 3.7 is used in two shapes: standalone via `Jamaa.Elsa.Server` + `Jamaa.Elsa.Studio`, and embedded inside the desktop app via `Jamaa.Desktop/Services/Hosting/`

---

## The Big Picture

### Layered Architecture

```
Jamaa.Desktop (Presentation)
    ├─ Views (.axaml) + ViewModels (.cs)
    ├─ MVVM via CommunityToolkit.Mvvm
    └─ Dispatches commands to application layer

Jamaa.Application (Use Cases & Orchestration)
    ├─ Aggregates (Akka.NET ReceiveActors)
    ├─ Commands (domain commands, records)
    ├─ Events (domain events, immutable)
    ├─ Services (facades, cross-cutting logic)
    └─ Configuration (DI registration)

Jamaa.Data (Persistence)
    ├─ Entity Framework Core + DbContext
    ├─ Repositories & Queries
    ├─ Akka.Persistence (SQLite backend)
    └─ Migrations

Jamaa.Domain (Pure Business Logic)
    ├─ Entities & Value Objects
    ├─ Domain Events
    ├─ Repository interfaces (no EF references)
    └─ NO external dependencies

Workflow tooling (solution-adjacent)
    ├─ Jamaa.Elsa.Server (standalone ASP.NET Core Elsa host)
    ├─ Jamaa.Elsa.Studio (Blazor WASM workflow designer)
    └─ Jamaa.Desktop/Services/Hosting (embedded Elsa + Studio host on loopback)
```

`Jamaa.WorkflowStudio/` exists in the repository but is not part of `Jamaa.sln`; treat it as inactive unless a task explicitly targets it.

### Why This Matters

**Domain is isolated**: If you need to add a new business rule, start in `Jamaa.Domain` with entities/events. Never put infrastructure details (EF, HTTP, file I/O) in Domain.

**Application orchestrates via actors**: Commands don't execute directly; they're sent to Akka actors (Aggregates) that apply them and emit domain events. This decouples command handling from storage.

**Desktop consumes via MVVM**: ViewModels dispatch commands and listen to events. UI changes flow through ViewModels, never directly from Application layer.

**Workflow authoring has two active hosts**: the standalone `Jamaa.Elsa.Server` + `Jamaa.Elsa.Studio` pair, and the desktop's embedded loopback host in `Jamaa.Desktop/Services/Hosting/`. When changing workflow APIs, auth, tenancy headers, or static asset hosting, compare `Jamaa.Elsa.Server/Program.cs`, `Jamaa.Elsa.Studio/Program.cs`, and `Jamaa.Desktop/Services/Hosting/ElsaWebApplicationBuilder.cs` together.

---

## The Integration/Operation Split (IOSP) — Critical Pattern

**Every method must be classified before writing code.** This is enforced through code review and affects testability.

### Integration Method
- **Role**: Orchestrates a business workflow by calling other methods
- **Characteristics**: 
  - Calls only application methods, no direct DB/HTTP/file access
  - Reads like a sequence of named steps
  - One statement per step; step names are intention-revealing
  - No `if/else` except trivial control flow around orchestration
  - Returns workflow output (usually result or DTO)
- **Example from codebase**: 
  ```csharp
  public async Task<Result<MemberRegistered>> RegisterMemberAsync(RegisterMember cmd)
  {
      var organisation = await LoadOrganisation(cmd.OrganisationId);  // step 1
      var newMember = CreateMemberAggreagate(cmd);                      // step 2
      var @event = await newMember.ApplyAsync(cmd);                     // step 3
      await PersistMember(newMember);                                   // step 4
      return Result.Success(@event);
  }
  ```
- **Test as**: Interaction/mock-based test; verify calls in order
- **Red flag**: Mixing workflow decisions with calculations/parsing

### Operation Method
- **Role**: Performs a single logical transformation, validation, lookup, or I/O action
- **Characteristics**:
  - Leaf-like; does one thing
  - No orchestration of other application methods
  - Can perform DB queries, HTTP calls, file access, event publishing
  - Can contain decision logic (scoring, validation, policy checks, mapping)
  - No dependencies on orchestration; just input → output
- **Example**:
  ```csharp
  private Member CreateMemberAggregate(RegisterMember command)
  {
      ValidateEmail(command.Email);
      return new Member(command.MemberId, command.Email, command.Name);
  }
  
  private async Task PersistMember(Member member)
  {
      _dbContext.Members.Add(member);
      await _dbContext.SaveChangesAsync();
  }
  ```
- **Test as**: Direct unit test with focused inputs
- **Red flag**: Calling multiple application-level methods

### Review Heuristics
A method is **likely hybrid** (and should be split) if:
- It calls application methods AND contains business predicates/calculations
- It calls application methods AND performs direct DB/HTTP/file work
- It mixes orchestration with parsing, mapping, or validation
- It contains multiple conceptual levels (workflow + low-level details)
- You can't summarize it as either "coordinates" or "computes/does I/O"

---

## Key Files & Patterns

### Akka Actors & Event Sourcing

**Location**: `Jamaa.Application/{Feature}/Aggregates/`

Aggregates are Akka `ReceiveActor` subclasses that:
1. Receive commands as messages
2. Validate state & command preconditions
3. Emit domain events (immutable records)
4. Apply events to update internal state

```csharp
public class OrganisationAggregate : ReceiveActor
{
    private Organisation _state;  // Mutable internal state
    
    public OrganisationAggregate(OrganisationId id, IQueryProcessor queries)
    {
        ReceiveAsync<CreateOrganisation>(OnCreateOrganisation);
        ReceiveAsync<RegisterMember>(OnRegisterMember);
    }
    
    private async Task OnCreateOrganisation(CreateOrganisation cmd)
    {
        var @event = new OrganisationCreated(cmd.OrganisationId, cmd.Name);
        Apply(@event);  // Update state
        PersistEvent(@event);  // Persist via Akka.Persistence
    }
    
    private void Apply(OrganisationCreated evt)
    {
        _state = new Organisation(evt.Id, evt.Name);
    }
}
```

**Why actors?** Event sourcing + immutability + natural concurrency model. Commands are serialized per actor; no race conditions.

### Commands (CQRS-style)

**Location**: `Jamaa.Application/{Feature}/Commands/`

Commands are immutable records representing user intent:

```csharp
public record RegisterMember(
    OrganisationId OrganisationId, 
    MemberId MemberId, 
    string Email, 
    string Name
);
```

Send to CommandProcessor (itself an Akka actor), which routes to the right aggregate.

### Domain Events

**Location**: `Jamaa.Application/{Feature}/Events/` or `Jamaa.Domain/{Feature}/`

Events are facts about what happened, immutable:

```csharp
public record MemberRegistered(MemberId MemberId, string Email, DateTime Timestamp);
```

Events are persisted via Akka.Persistence and can trigger side effects (send notifications, update read models, etc.).

### MVVM ViewModels

**Location**: `Jamaa.Desktop/{Feature}/` (views and viewmodels co-located)

```csharp
public partial class MembersViewModel : ObservableObject
{
    private readonly ICommandDispatcher _commands;
    
    [ObservableProperty]
    private ObservableCollection<MemberViewModel> members;
    
    [RelayCommand]
    public async Task RegisterMember(string email, string name)
    {
        var cmd = new RegisterMember(_orgId, MemberId.New(), email, name);
        var result = await _commands.SendAsync(cmd);
        if (result.IsSuccess)
            await RefreshMembers();
    }
    
    private async Task RefreshMembers()
    {
        var query = new GetMembers(_orgId);
        Members = new(await _queryProcessor.ExecuteAsync(query));
    }
}
```

**Key conventions**:
- Use `[ObservableProperty]` attribute on private fields (auto-generates properties)
- Private field naming: `_fieldName`
- UI dispatch: `Dispatcher.UIThread.InvokeAsync()` for off-thread updates
- Commands sent to application layer; listen to events for updates
- Enforce strict MVVM boundaries:
  - ViewModels expose state, validation, and commands only
  - Do not instantiate controls or compose templates in ViewModels
  - Do not add ViewModel methods that configure/attach `IDataTemplate` columns (e.g., `ConfigureActionCellTemplates`)
  - Do not assign style classes, brushes, or other visual resources in ViewModels
  - Keep control templates/classes/visual states in `.axaml` shared styles/themes (or minimal view code-behind when required by framework limits)
  - Prefer `DataTemplate`/selector/behavior (or view-local helper) for UI template composition

### Dependency Injection & Service Registration

**Location**: `Jamaa.Application/Configuration/ApplicationServicesRegistration.cs`

Services are registered with interceptors (for authorization, logging):

```csharp
services.AddProxiedScoped<IUserManagementFacade, UserManagementFacade>();
```

Uses Castle DynamicProxy to wrap interfaces with authorization checks.

### Embedded Elsa Host

**Location**: `Jamaa.Desktop/Services/Hosting/`

The desktop app starts an in-process workflow host during `InitializationService.InitializeAsync()`:

- `EmbeddedWebServer` starts before other background services complete startup and exposes `Started`, `BaseAddress`, and `Port`
- `ElsaWebApplicationBuilder` hosts Elsa APIs and the Studio WASM assets on `127.0.0.1` using a dynamic port
- Elsa persistence uses sibling SQLite files (`elsa-management.db`, `elsa-runtime.db`) next to the main Jamaa SQLite database
- Tenancy for embedded workflows is routed via the `x-tenant` header in `ElsaWebApplicationBuilder`

When changing embedded workflow behavior, inspect `EmbeddedWebServer.cs`, `ElsaWebApplicationBuilder.cs`, `ElsaDatabaseInitializer.cs`, and `InitializationService.cs` together.

---

## Naming & Coding Conventions

- **C# 14 features**: Primary constructors, file-scoped namespaces, collection expressions are expected
- **Prefer records for immutable data shapes**: Use `record`/`record struct` for value-like, immutable carriers (commands, events, DTOs, query results, passive domain state) when identity and mutation are not required. Use `class` when the type has lifecycle/identity semantics, encapsulated mutable state, framework proxy/materialization constraints, or behavior-heavy responsibilities.
- **PascalCase**: Classes, methods, public properties
- **camelCase with `_` prefix**: Private fields (`_member`, not `member`)
- **`nameof` over strings**: Always `nameof(MyMethod)`, never `"MyMethod"`
- **`var` when obvious**: `var list = new List<int>();` ✓ but `List<int> list = ...` ✗
- **Async all the way**: I/O-bound operations must be `async/await`, never `.Result` or `.Wait()`

---

## Adding a New Feature/Module

1. **Domain** → Define entities, value objects, events in `Jamaa.Domain/{Feature}/`
2. **Application** → 
   - Create commands in `Commands/`
   - Create aggregate/actor in `Aggregates/`
   - Create facade service if orchestration needed
   - Register in `ApplicationServicesRegistration.cs`
3. **Data** → Create EF models, repositories, migrations in `Jamaa.Data/`
4. **Desktop** → Create views (`.axaml`) + viewmodels (`.cs`) in `Jamaa.Desktop/{Feature}/`
5. **Tests** → Mirror the feature path in `Tests/` with BDD (`.feature`) or unit tests

---

## End-to-End: Adding a New Event Type

When adding a new event (e.g., `AccountOpeningBalanceSet`), you must ensure it flows through the entire system. Missing one step will result in the UI not updating.

### Step 1: Define Command & Event
- **Location**: `Jamaa.Application/{Feature}/Commands/` and `Events/` (or `Jamaa.Domain`)
- **Action**: Define immutable records for the intent (Command) and the fact (Event).

### Step 2: Handle Command in Aggregate
- **Location**: `Jamaa.Application/{Feature}/Aggregates/`
- **Action**:
  - Add `ReceiveAsync<TCommand>(handler)` to constructor.
  - Implement handler: validate, create event, call `PersistEvent(@event)`.
  - Add `Apply(@event)` method to update internal actor state.

### Step 3: Tag the Event for Projections (CRITICAL)
- **Location**: `Jamaa.Application/Shared/JamaaEventTagger.cs`
- **Action**: Add the event to the `ToJournal` switch statement. 
- **Why**: Projections filter events by tags (e.g., `OrganisationEvent`, `FinanceChanged`). If not tagged, the projection will never see the event.

### Step 4: Update Read Model (Persistence)
- **Location**: `Jamaa.Data/Configuration/JamaaDbContext.cs` and `Jamaa.Data/Models/`
- **Action**:
  - Add or update the POCO model in `Jamaa.Data/Models/`.
  - Add a `DbSet<TModel>` to `JamaaDbContext`.
  - Configure the mapping in `OnModelCreating` (or via `IEntityTypeConfiguration`).
  - Generate a migration: `dotnet ef migrations add MyNewChange`.

### Step 5: Implement Projection Handler
- **Location**: `Jamaa.Application/Shared/JamaaEventProjection.cs`
- **Action**:
  - Implement `Task Handle(TEvent @event, JamaaDbContext dbContext)`.
  - Register it in `RegisterEventHandlers()`: `ReceiveAsync<TEvent>(e => Handle(e, dbContext))`.
- **Note**: This is where the event is transformed into a database row update.

### Step 6: Expose via Query & UI
- **Location**: `Jamaa.Data/Queries/` and `Jamaa.Desktop/{Feature}/`
- **Action**:
  - Create/Update a query handler to read the new data from `JamaaDbContext`.
  - Update the ViewModel to execute the query and expose the result as an `[ObservableProperty]`.
  - Update the View (`.axaml`) to bind to the new property.

---

## Build & Deployment Workflows

### Local Development
```bash
# Restore local tools (required before running EF commands)
dotnet tool restore

# Build
dotnet build

# Run tests (BDD + unit)
dotnet test

# Run desktop app
dotnet run --project Jamaa.Desktop/Jamaa.Desktop.csproj

# Run standalone Elsa host + Studio
dotnet run --project Jamaa.Elsa.Server --urls https://localhost:5001
```

Desktop startup already launches the embedded Elsa host via `Jamaa.Desktop/Services/InitializationService.cs`; use `Jamaa.Elsa.Server` only when working on the standalone workflow host or Studio shell directly.

### Packaging (macOS)
```powershell
# From project root, run the bundle script
./bundle/package-macos.ps1
```

Produces:
- `publish/` directory with compiled binaries
- `Jamaa.app` bundle structure
- `Jamaa-Installer.dmg` for distribution

**Key flags** in publish command:
- `-c Release` → Optimized production build
- `-r osx-x64` → Intel macOS target (changeable to `osx-arm64` for Apple Silicon)
- `--self-contained true` → Bundles .NET runtime (no user installation needed)
- `-p:PublishReadyToRun=true` → Ahead-of-time compilation for faster startup

---

## Testing Strategy

### Unit Tests
Located in `Tests/` mirroring the project structure:
- The runnable automated suite is `Tests/UnitTests.csproj`
- Test **Operations** directly with focused inputs
- Mock dependencies; verify behavior
- Prefer the existing xUnit + Shouldly + NSubstitute style already used in `Tests/Finances/` and `Tests/Services/`
- Fast, isolated, deterministic

### BDD (Gherkin)
Located in `Tests/` as `.feature` files:
- `Registration.feature` is currently a requirements artifact in the repo
- Automated coverage is primarily xUnit-based today; no active SpecFlow test project is present in `Jamaa.sln`
- `IntegrationTests/` exists under `Tests/` but is excluded from compilation in `Tests/UnitTests.csproj` unless you intentionally re-include it

**Exception translation**: Operations detect errors; integrations translate them at workflow boundaries if needed.

---

## Common Pitfalls & Fixes

| Pitfall | Why It Breaks | Fix |
|---------|---------------|-----|
| Method mixes orchestration + logic | Hard to test; violates SRP | Split: integration calls operation |
| Direct DB access in ViewModel | Breaks MVVM; hard to test | Use command/query through app layer |
| ViewModel creates/styles UI controls or templates | Breaks MVVM separation; mixes presentation with behavior | Move control/template/class/resource definitions to view markup/shared styles |
| ViewModel exposes `Configure*Template*` methods for UI composition | Leaks presentation composition into VM API | Move template selection/composition to `.axaml`, selector/behavior, or view code-behind |
| Synchronous I/O (`.Result`, `.Wait()`) | Deadlocks; UI freeze | Always `await` |
| Shared mutable state across actors | Race conditions | Actors own state; pass data by value |
| Actor processing multiple message types without order guarantee | Event order matters | Use single queue per aggregate; batch if needed |
| Inline XAML styles | Not reusable; maintenance nightmare | Use `Jamaa.Desktop/Styles/` or `Jamaa.Desktop/Themes/` |

---

## External Dependencies & Integration Points

- **Avalonia UI**: Cross-platform; Skia renderer
- **Akka.NET**: Actor model, distributed (currently single-node SQLite backend)
- **Entity Framework Core**: ORM for read models, domain persistence
- **Elsa Workflows / Elsa Studio**: Workflow engine + designer used both as standalone projects and inside the desktop embedded host
- **CommunityToolkit.Mvvm**: ObservableProperty, RelayCommand generators
- **Castle DynamicProxy**: Runtime interception for auth/logging
- **Serilog**: Shared structured logging across desktop startup, Akka, and the embedded workflow host
- **Syncfusion**: License-backed UI components; see `Syncfusion` settings in desktop app settings before assuming components can initialize cleanly

---

## Architecture Decision Records (implicit)

1. **Why actors?** Immutability, natural concurrency, event sourcing compatibility
2. **Why IOSP?** Clear testability boundary; prevents god methods
3. **Why DDD?** Ubiquitous language; domain logic stays pure & frontend-agnostic
4. **Why MVVM?** Separation of UI logic from business logic; reusability
5. **Why Akka.Persistence?** Audit trail, recovery, temporal queries, natural replay

---

## For AI Agents: Immediate Productivity Checklist

When assigned a task:
- [ ] Identify which layer(s) the work touches (Domain? Application? Desktop?)
- [ ] If adding application logic: **Create operations first, then integration**
- [ ] If adding UI: Create view + viewmodel pair; use existing styles
- [ ] Enforce strict MVVM: no control/template/style composition in ViewModels
- [ ] If touching workflows: inspect both standalone and embedded hosts (`Jamaa.Elsa.Server`, `Jamaa.Elsa.Studio`, `Jamaa.Desktop/Services/Hosting/`)
- [ ] If modifying commands: Update aggregate handlers + tests
- [ ] If adding a domain event: Add handler in aggregate `Apply()` method
- [ ] **Crucial**: Tag new events in `JamaaEventTagger.cs` for projection
- [ ] **Crucial**: Implement handler in `JamaaEventProjection.cs` for read model updates
- [ ] Register new services in `ApplicationServicesRegistration`
- [ ] Mirror test structure in `Tests/` directory
- [ ] Verify with the existing xUnit suite in `Tests/UnitTests.csproj`; do not assume `IntegrationTests/` is compiled
- [ ] Use `var`, `nameof()`, async/await consistently
- [ ] Ask: "Is this method Integration or Operation?" before writing

---

## References

- **Existing guidelines**: See `.junie/guidelines.md` for JetBrains-specific notes
- **Workflow setup**: See `ELSA_SETUP.md` and `ELSA_EMBEDDED_INTEGRATION.md` for standalone vs embedded Elsa details
- **Product idea**: `Product Idea.md` documents business requirements
- **Project structure**: See `Jamaa.sln` and workspace folders

