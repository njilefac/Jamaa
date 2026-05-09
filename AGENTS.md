# AGENTS.md — Jamaa Development Guide for AI Assistants

This guide helps AI coding agents understand the Jamaa codebase architecture, conventions, and workflows. Last updated: April 2026.

---

## Quick Facts

- **Tech Stack**: .NET 10, C# 14, Avalonia UI (cross-platform desktop), Akka.NET (actors), Entity Framework Core
- **Architecture**: Clean Architecture + Domain-Driven Design (DDD)
- **Build Target**: macOS first (packaged via PowerShell scripts in `/bundle`), cross-platform UI
- **Key Pattern**: Integration/Operation split (IOSP) for all methods; Command-Query separation via actors

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
```

### Why This Matters

**Domain is isolated**: If you need to add a new business rule, start in `Jamaa.Domain` with entities/events. Never put infrastructure details (EF, HTTP, file I/O) in Domain.

**Application orchestrates via actors**: Commands don't execute directly; they're sent to Akka actors (Aggregates) that apply them and emit domain events. This decouples command handling from storage.

**Desktop consumes via MVVM**: ViewModels dispatch commands and listen to events. UI changes flow through ViewModels, never directly from Application layer.

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

## Build & Deployment Workflows

### Local Development
```bash
# Build
dotnet build

# Run tests (BDD + unit)
dotnet test

# Run desktop app
dotnet run --project Jamaa.Desktop/Jamaa.Desktop.csproj
```

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
- Test **Operations** directly with focused inputs
- Mock dependencies; verify behavior
- Fast, isolated, deterministic

### BDD (Gherkin)
Located in `Tests/` as `.feature` files:
- High-level functional requirements
- Example: "Given an organisation exists, When a member registers, Then the member is listed"
- Implements step definitions in C# using SpecFlow

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
- **CommunityToolkit.Mvvm**: ObservableProperty, RelayCommand generators
- **Castle DynamicProxy**: Runtime interception for auth/logging

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
- [ ] If modifying commands: Update aggregate handlers + tests
- [ ] If adding a domain event: Add handler in aggregate `Apply()` method
- [ ] Register new services in `ApplicationServicesRegistration`
- [ ] Mirror test structure in `Tests/` directory
- [ ] Use `var`, `nameof()`, async/await consistently
- [ ] Ask: "Is this method Integration or Operation?" before writing

---

## References

- **Existing guidelines**: See `.junie/guidelines.md` for JetBrains-specific notes
- **Product idea**: `Product Idea.md` documents business requirements
- **Project structure**: See `Jamaa.sln` and workspace folders

