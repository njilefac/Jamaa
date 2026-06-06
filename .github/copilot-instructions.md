# GitHub Copilot Instructions for Jamaa

This file provides GitHub Copilot with essential context for the Jamaa codebase.

For comprehensive architecture, patterns, and workflow details, see the root-level `AGENTS.md` file.
For clean code, design principles, and implementation heuristics, also apply `.junie/guidelines.md`.

## Instruction Sources

- `AGENTS.md` is the primary architecture and workflow guide.
- `.junie/guidelines.md` provides additional clean code, design, and implementation principles.
- When both apply, follow both together and prefer the stricter clean architecture / IOSP interpretation.

## Quick Context

- **Project**: Jamaa — association management app, .NET 10 + Avalonia UI
- **Architecture**: Clean Architecture + DDD with Akka.NET actors
- **Key Pattern**: Integration/Operation split (IOSP) — classify every method before writing

## Core Coding Rules

1. **Integration vs Operation**: Label every method before writing
   - **Integration**: Orchestrates other methods; reads like a workflow
   - **Operation**: Performs one action (DB, compute, validation, I/O)
   - Split hybrid methods

2. **Language**: C# 14 — use primary constructors, `var`, `nameof()`, async/await everywhere

3. **UI (Avalonia MVVM)**
   - Views (.axaml) + ViewModels (.cs) in `Jamaa.Desktop/{Feature}/`
   - Use `[ObservableProperty]` on private fields
   - Dispatch commands through application layer, never direct DB access
   - **Strict MVVM enforcement**:
     - ViewModels must not create or style UI controls (`Button`, `ToggleSwitch`, etc.)
     - ViewModels must not assign UI classes, templates, or visual resources
     - ViewModels must not expose methods that wire `IDataTemplate`/`DataTemplate` columns (for example `Configure*Template*` methods)
     - Prefer template selection/composition in `.axaml` via `DataTemplate`, selector, behavior, or view code-behind helper for all UI composition needs
     - All control composition, templates, classes, and visual states belong in `.axaml` (or view code-behind only when unavoidable)
     - ViewModels expose state/commands only; views own presentation details

4. **Application Logic (Akka Actors)**
   - Aggregates in `Jamaa.Application/{Feature}/Aggregates/`
   - Commands in `Commands/`; Events in `Events/`
   - Aggregates receive commands, validate, emit events, apply state changes

5. **Domain Layer**
   - Entities, value objects, events in `Jamaa.Domain/{Feature}/`
   - Zero external dependencies (no EF, HTTP, file I/O)

6. **Naming**: 
   - PascalCase: classes, methods, public properties
   - camelCase with `_` prefix: private fields

7. **Async Return Types**:
   - Never use `async void` methods
   - Use `Task` or `Task<T>` instead
   - If fire-and-forget is required, use an explicit non-`async void` wrapper that handles errors

## Clean Code & Design Principles

- Apply the clean code and design guidance from `.junie/guidelines.md` in all generated code.
- Keep classes and methods focused on one responsibility.
- Prefer intention-revealing names over comments that explain unclear code.
- Keep integrations thin and workflow-oriented; push validation, mapping, calculations, persistence, and policy checks into operations.
- Avoid hybrid methods that both coordinate workflows and perform detailed logic.
- Prefer data handoff between steps over shared mutable state where practical.
- Keep domain code pure and free from infrastructure/framework concerns.
- In UI code, preserve MVVM separation and avoid inline styles when shared styles/themes already exist.
- Treat MVVM boundary violations as blockers: if a ViewModel contains presentation composition, refactor it into view markup/resources.
- In actor-based code, prefer immutable messages, focused actors, and explicit state transitions.
- If a method or class feels like it has multiple conceptual levels, split it.

## When Adding Code

- Start with **Integration/Operation classification**
- If domain logic: define in `Jamaa.Domain`
- If app orchestration: integrate in `Jamaa.Application`
- If UI: add to `Jamaa.Desktop` views + viewmodels
- Register services in `ApplicationServicesRegistration.cs`
- Mirror tests in `Tests/` directory

## Common Patterns in This Codebase

- **Command handling**: Command sent to CommandProcessor actor → routed to aggregate
- **Event sourcing**: Aggregates apply events to update state; events persisted via Akka.Persistence
- **Error handling**: Operations detect/raise detailed errors; integrations translate at boundaries
- **Testing**: Operations = focused unit tests; Integrations = interaction/mock-based tests

## References

For more detail on architecture decisions, external dependencies, and development workflows, consult `AGENTS.md` in the project root.
For clean code rationale, design heuristics, and additional implementation guidance, consult `.junie/guidelines.md`.

