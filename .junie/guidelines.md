# Jamaa Development Guidelines

## Project Overview
Jamaa is an association management application built with .NET 10, C# 14, and Avalonia UI. It follows Clean Architecture and Domain-Driven Design (DDD) principles.

## Architecture
- **Jamaa.Domain**: Core domain models (Entities, Value Objects), Domain Events, and Repository interfaces. Keep it free of external dependencies.
- **Jamaa.Application**: Application logic, Use Cases (Aggregates, Commands), and Services. Uses Akka.NET for actor-based logic and event sourcing.
- **Jamaa.Data**: Data persistence using Entity Framework Core and Akka.Persistence (Sqlite by default).
- **Jamaa.Desktop**: Presentation layer using Avalonia UI and MVVM.

## Coding Standards
- **C# 14 Features**: Leverage latest language features such as primary constructors, file-scoped namespaces, and collection expressions.
- **MVVM**: Use `CommunityToolkit.Mvvm` for ViewModels (`ObservableObject`, `ObservableProperty`, etc.).
- **Dependency Injection**: Register services in `ApplicationServicesRegistration` or `PresentationServicesRegistration`.
- **Naming**: 
  - PascalCase for classes, methods, and public properties.
  - camelCase with underscore prefix (`_field`) for private fields, especially when used with `[ObservableProperty]`.
- **Asynchronous Code**: Prefer `async/await` for all I/O-bound operations.
- **Async Return Types**: Never use `async void`; use `Task`/`Task<T>` instead. If fire-and-forget is required, use an explicit wrapper that handles exceptions.
- **Single Responsibility Principle**: Each class should have one reason to change. Keep methods focused and concise.
- always prefer `nameof` over string literals.
- always prefer `var` when the type is obvious from the right-hand side.
- avoid inline styles for XAML and use shared styles and themes instead.

## Every new method/function must be classified as either Integration or Operation (IOSP).
Integration methods orchestrate only by calling other application methods and must not contain business logic.
Operation methods perform logic or external I/O and must not orchestrate other application methods.
Avoid hybrid methods.


- Label the role mentally before writing code
- Before generating a method, decide: Integration or Operation.
- If the role is unclear, split the method.
- Integration methods should read like workflows
- Their body should look like a sequence of named steps.
- Prefer one statement per step.
- Push logic down into operations with intention-revealing names.
- Operation methods should be leaf-like
- They should do one logical transformation, validation, lookup, persistence action, or external call.
- They should not coordinate a business process.
- No hybrids
- If a method both decides what happens and also performs detailed work, split it.
- Decision logic belongs in operations
- if, switch, complex boolean predicates, scoring, mapping, calculations, normalization, policy checks: put them in operations.
- An integration may branch only for trivial control flow around orchestration, and even that should be minimized.
- External side effects belong in operations
- DB queries, HTTP calls, file system access, message publishing, clock access, GUID generation, framework APIs: keep them in operations or adapters.
- The integration should call a named operation like LoadCustomer, PersistOrder, PublishOrderSubmitted.
- Integrations should be thin
- As a heuristic, an integration method should usually be short enough to view at once and mostly consist of calls.
- If it needs comments to explain the flow, the step names are probably wrong.
- Prefer data handoff over shared mutation
- Let operations return values.
- Integrations pass outputs from one step into the next.
- Avoid giant mutable state objects passed through many steps unless unavoidable.
- Exceptions and error translation
- Let operations detect or raise detailed errors.
- Let integrations translate them only at workflow boundaries if needed.
- Testing strategy follows the split
- Test operations directly with focused unit tests.
- Test integrations with interaction-oriented tests or high-value workflow tests.
- Review heuristics the agent can apply automatically
- It calls application methods and contains business predicates/calculations.
- It calls application methods and performs direct DB/HTTP/file work.
- It mixes orchestration with parsing, mapping, validation, or policy checks.
- It contains several conceptual levels at once: workflow steps plus low-level details.
- You cannot summarize the method as either “coordinates” or “computes/does I/O.”

## UI Guidelines (Avalonia UI)
- Maintain strict separation between View (`.axaml`) and ViewModel (`.cs`).
- Nest the view models under the corresponding view.
- Use the shared styles in `Jamaa.Desktop/Styles` and themes in `Jamaa.Desktop/Themes`.
- Icons and resources should be placed in `Jamaa.Desktop/Assets`.

## Testing
- **Unit Tests**: Located in the `Tests` project, mirroring the project structure of the code under test.
- **BDD (Gherkin)**: Use `.feature` files for high-level functional requirements.

## Akka.NET & Event Sourcing
- This project uses Akka.NET actors for complex business logic and state management.
- Aggregates in `Jamaa.Application` often correspond to Persistent Actors.
- Follow actor model best practices: small, focused actors; immutable messages; and proper supervision strategies.

## Adding a New Module
1. Define Domain entities and events in `Jamaa.Domain`.
2. Implement application logic (aggregates, commands, actors) in `Jamaa.Application`.
3. Add data persistence if needed in `Jamaa.Data`.
4. Create the UI in `Jamaa.Desktop` using `.axaml` for views and `.cs` for ViewModels.
5. Register new services in the appropriate registration class.
