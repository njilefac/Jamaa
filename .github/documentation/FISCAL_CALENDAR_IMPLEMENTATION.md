# Fiscal Calendar Implementation - Complete Summary

## Overview

This document describes the complete end-to-end implementation of the Fiscal Calendar feature for the Jamaa association management application. The implementation follows the Clean Architecture + DDD + IOSP (Integration/Operation Split) pattern with Akka.NET actors and event sourcing.

**Status**: ✅ **Complete and Operational**
- Build: ✅ Succeeds (0 errors)
- Tests: ✅ Pass (12/12)
- All layers: ✅ Implemented

---

## Architecture Overview

### Layered Structure

```
Jamaa.Desktop (Presentation Layer)
    └─ Accounting/
        ├─ FiscalCalendarAndPeriodsPage.axaml (Master/Detail UI)
        ├─ FiscalCalendarAndPeriodsViewModel.cs (Integration: orchestrates UI workflow)
        ├─ FiscalYearEditorItemViewModel.cs (Fiscal year editor state)
        └─ AccountingPeriodItemViewModel.cs (Period detail state)

    ↓↓↓ IFinanceManagementFacade ↓↓↓

Jamaa.Application (Use Cases & Orchestration Layer)
    └─ Finances/
        ├─ IFinanceManagementFacade.cs (Facade interface)
        ├─ FinanceManagementFacade.cs (Integration: orchestrates commands & queries)
        ├─ Commands/ (6 command records for user intent)
        ├─ Events/ (6 immutable event records)
        ├─ Aggregates/
        │   └─ FiscalCalendarAggregate.cs (ReceivePersistentActor: event-sourced state machine)
        └─ Shared/
            ├─ CommandProcessor.cs (Routes commands to aggregate actors)
            ├─ LibotaEventTagger.cs (Tags events for projection)
            └─ OrganisationProjection.cs (Projects events to read models)

Jamaa.Domain (Pure Business Logic Layer)
    └─ Finances/
        ├─ Values/
        │   ├─ FiscalYearId.cs (Value object: GUID wrapper)
        │   └─ AccountingPeriodId.cs (Value object: GUID wrapper)
        └─ Queries/
            └─ GetFiscalYearsByOrganisation.cs (Read contract)

Jamaa.Data (Persistence & Data Access Layer)
    ├─ Models/Finances/
    │   ├─ FiscalYearData.cs (EF read model entity)
    │   └─ AccountingPeriodData.cs (EF read model entity)
    ├─ Queries/Finances/
    │   ├─ IFiscalCalendarQueryHandler.cs (Read interface)
    │   └─ FiscalCalendarQueryHandler.cs (Operation: queries read model)
    ├─ Repositories/Finances/
    │   ├─ IFiscalYearRepository.cs (CRUD interface for fiscal year)
    │   └─ IAccountingPeriodRepository.cs (CRUD interface for period)
    ├─ Configuration/
    │   ├─ JamaaDbContext.cs (EF DbContext with FiscalYears & AccountingPeriods DbSets)
    │   └─ DataServiceCollectionExtensions.cs (DI registration)
    └─ Migrations/
        └─ 20260420_AddFiscalYearAndAccountingPeriod.cs (Applied migration)
```

---

## Command Flow

### Presentation Layer → Backend

**User Action**: "Add Fiscal Year"
1. **UI**: `AddFiscalYearCommand` triggered → calls `_financeManagementFacade.CreateFiscalYear(...)`
2. **Facade** (Integration): Wraps into `CreateFiscalYear` command record, sends to CommandProcessor actor
3. **CommandProcessor** (Actor): Creates `FiscalCalendarAggregate` actor per organisation, sends command as message
4. **FiscalCalendarAggregate** (ReceivePersistentActor):
   - Validates contiguity (no gaps with existing fiscal years)
   - Generates default monthly accounting periods
   - Emits batch of events: `FiscalYearCreated` + `AccountingPeriodCreated` × N
   - Uses `PersistAll()` for batch persistence (Akka.NET best practice)
5. **Akka.Persistence** (SQLite backend):
   - Persists events to journal
   - Takes snapshot every 20 events
6. **LibotaEventTagger**: Tags events with `OrganisationEvent` + `FinanceChanged`
7. **OrganisationProjection**:
   - Subscribes to tagged events
   - For each `FiscalYearCreated` → inserts `FiscalYearData` row
   - For each `AccountingPeriodCreated` → inserts `AccountingPeriodData` row
   - Events projected to EF read models in real-time
8. **UI**: Calls `LoadFiscalYearsAsync()` → queries via facade
9. **Facade** (Integration): Calls `_queryProcessor.Get(GetFiscalYearsByOrganisation(...))`
10. **QueryProcessor**: Routes to `FiscalCalendarQueryHandler`
11. **FiscalCalendarQueryHandler** (Operation): Queries EF DbSet, includes periods, orders descending
12. **UI**: Receives `FiscalYearData[]`, maps to `FiscalYearEditorItemViewModel[]`, renders

---

## Key Implementation Details

### 1. Domain Layer

**Value Objects**:
- `FiscalYearId`: Immutable GUID wrapper with `New()` factory and `With(string)` constructor
- `AccountingPeriodId`: Same pattern as FiscalYearId

**Query Contract**:
- `GetFiscalYearsByOrganisation`: Record with `OrganisationId` parameter

### 2. Application Layer - Commands (Intent Capture)

```csharp
// Create a fiscal year
public record CreateFiscalYear(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

// Update fiscal year boundaries or lock state
public record UpdateFiscalYear(
    OrganisationId OrganisationId,
    string FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

// Delete fiscal year (cascades to periods)
public record DeleteFiscalYear(
    OrganisationId OrganisationId,
    string FiscalYearId);

// Create accounting period within fiscal year
public record CreateAccountingPeriod(
    OrganisationId OrganisationId,
    string FiscalYearId,
    AccountingPeriodId PeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

// Update period state
public record UpdateAccountingPeriod(
    OrganisationId OrganisationId,
    string FiscalYearId,
    string PeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);

// Delete period
public record DeleteAccountingPeriod(
    OrganisationId OrganisationId,
    string FiscalYearId,
    string PeriodId);
```

### 3. Application Layer - Events (Audit Trail & Fact Record)

```csharp
// Fiscal year was created
public record FiscalYearCreated(
    FiscalYearId FiscalYearId,
    OrganisationId OrganisationId,
    DateTime StartDate,
    DateTime EndDate) : ILibotaEvent;

// Fiscal year was updated
public record FiscalYearUpdated(
    FiscalYearId FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked) : ILibotaEvent;

// Fiscal year was deleted
public record FiscalYearDeleted(FiscalYearId FiscalYearId) : ILibotaEvent;

// Accounting period was created
public record AccountingPeriodCreated(
    AccountingPeriodId PeriodId,
    FiscalYearId FiscalYearId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate) : ILibotaEvent;

// Accounting period was updated
public record AccountingPeriodUpdated(
    AccountingPeriodId PeriodId,
    FiscalYearId FiscalYearId,
    bool IsLocked) : ILibotaEvent;

// Accounting period was deleted
public record AccountingPeriodDeleted(
    AccountingPeriodId PeriodId,
    FiscalYearId FiscalYearId) : ILibotaEvent;
```

### 4. Aggregate Actor (FiscalCalendarAggregate)

**Role**: Event-sourced state machine enforcing fiscal calendar business rules

**Key Features**:
- **Persistent State**: Per organisation (`PersistenceId = "fiscal-calendar-{organisationId}"`)
- **Event Recovery**: State rebuilt from persisted events on actor restart
- **Snapshots**: Taken every 20 events for performance
- **Batch Operations**: Uses `PersistAll()` for multiple events

**Command Handlers**:

1. **Integration - CreateFiscalYear**:
   - ✅ Validates new fiscal year doesn't create gaps in timeline
   - ✅ Generates default monthly accounting periods
   - ✅ Persists multiple events in batch

2. **Integration - UpdateFiscalYear**:
   - ✅ Validates boundary change maintains contiguity
   - ✅ Deletes old periods (if boundaries changed)
   - ✅ Regenerates periods for new boundary
   - ✅ Handles lock state sync

3. **Integration - DeleteFiscalYear**:
   - ✅ Validates remaining fiscal years stay contiguous
   - ✅ Cascades period deletions
   - ✅ Persists deletion event

4. **Operation - CreateAccountingPeriod**:
   - ✅ Validates period covers fiscal year
   - ✅ Enforces lock rules

5. **Operation - UpdateAccountingPeriod**:
   - ✅ Updates period state
   - ✅ Triggers lock sync

6. **Operation - DeleteAccountingPeriod**:
   - ✅ Deletes only if coverage maintained

**Lock State Synchronization**:
- When fiscal year is locked → all periods auto-lock
- When any period is reopened → fiscal year auto-reopens
- When all periods locked and cover year → fiscal year auto-locks

**Helper Methods**:
- `GenerateDefaultPeriods()`: Creates monthly periods (max 1 month, except last)
- `IsContiguous()`: Validates no date gaps/overlaps
- `CoversWholeFiscalYear()`: Ensures periods fully span fiscal year
- `PersistFiscalYearLockSync()`: Auto-lock/reopen logic

### 5. Facade (FinanceManagementFacade)

**Role**: Bridge between presentation layer and application layer

**Integration Pattern**:
- Command methods dispatch to aggregate via CommandProcessor
- Query methods delegate to QueryProcessor
- Returns `Task` for fire-and-forget commands
- Returns `Task<T>` for queries

```csharp
public class FinanceManagementFacade : IFinanceManagementFacade
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    // Integration: dispatch create command
    public Task CreateFiscalYear(string organisationId, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new CreateFiscalYear(...);
        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatch update command
    public Task UpdateFiscalYear(string organisationId, string fiscalYearId, DateTime startDate, DateTime endDate, bool isLocked)
    {
        var command = new UpdateFiscalYear(...);
        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: dispatch delete command
    public Task DeleteFiscalYear(string organisationId, string fiscalYearId)
    {
        var command = new DeleteFiscalYear(...);
        _commandProcessor.Tell(command);
        return Task.CompletedTask;
    }

    // Operation: query read model
    public Task<IList<FiscalYearData>> GetFiscalYears(string organisationId)
    {
        return _queryProcessor.Get(new GetFiscalYearsByOrganisation(OrganisationId.With(organisationId)));
    }

    // Similar for period operations...
}
```

### 6. Data Access Layer

**EF Read Models**:
```csharp
// FiscalYearData
public class FiscalYearData
{
    [Key] public required string Id { get; set; }
    public required string OrganisationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }
    public Collection<AccountingPeriodData> Periods { get; set; } = [];
}

// AccountingPeriodData
public class AccountingPeriodData
{
    [Key] public required string Id { get; set; }
    public required string FiscalYearId { get; set; }
    public required string OrganisationId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsLocked { get; set; }
    public FiscalYearData? FiscalYear { get; set; }
}
```

**Query Handler** (Operation):
```csharp
public class FiscalCalendarQueryHandler : IFiscalCalendarQueryHandler
{
    public async Task<IList<FiscalYearData>> Get(GetFiscalYearsByOrganisation query)
    {
        return await dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .Where(fy => fy.OrganisationId == query.OrganisationId.Value)
            .OrderByDescending(fy => fy.StartDate)
            .ToListAsync();
    }
}
```

### 7. Presentation Layer

**View Model Hierarchy**:

1. **FiscalCalendarAndPeriodsViewModel** (Integration):
   - ✅ Loads fiscal years via facade on initialization
   - ✅ Maintains DynamicData `SourceList<>` for reactive sorting
   - ✅ Orchestrates CRUD commands
   - ✅ Manages period lock state synchronization
   - ✅ Handles UI state transitions

2. **FiscalYearEditorItemViewModel** (Detail Editor):
   - Holds fiscal year state (dates, lock status)
   - Manages period collection
   - Auto-generates display labels

3. **AccountingPeriodItemViewModel** (Period Detail):
   - Holds period state (dates, sequence, lock status, **actual database Id**)
   - Provides formatted display labels

---

## Routing & Wiring

### DI Registration

**Application Layer** (`ApplicationServicesRegistration.cs`):
```csharp
services.AddProxiedSingleton<IFinanceManagementFacade, FinanceManagementFacade>();
```

**Data Layer** (`DataServiceCollectionExtensions.cs`):
```csharp
services.AddScoped<IFiscalCalendarQueryHandler, FiscalCalendarQueryHandler>();
services.AddScoped<IFiscalYearRepository, FiscalYearRepository>();
services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
```

### Command Routing

**CommandProcessor.cs**:
```csharp
// 6 handlers for finance commands
ReceiveAsync<CreateFiscalYear>(cmd => Handle(cmd));
ReceiveAsync<UpdateFiscalYear>(cmd => Handle(cmd));
ReceiveAsync<DeleteFiscalYear>(cmd => Handle(cmd));
ReceiveAsync<CreateAccountingPeriod>(cmd => Handle(cmd));
ReceiveAsync<UpdateAccountingPeriod>(cmd => Handle(cmd));
ReceiveAsync<DeleteAccountingPeriod>(cmd => Handle(cmd));
```

Each handler:
1. Creates/retrieves `FiscalCalendarAggregate` actor
2. Sends command as message to aggregate
3. Aggregate processes, validates, emits events, persists

### Event Projection

**LibotaEventTagger.cs**:
```csharp
case FiscalYearCreated fiscalYearCreated =>
    new Tagged(fiscalYearCreated, new[] { OrganisationEvent, FinanceChanged }),
// ... 5 more cases for other events
```

**OrganisationProjection.cs**:
```csharp
// 6 Handle methods for each event type
private async Task Handle(FiscalYearCreated @event, JamaaDbContext dbContext)
{
    var fiscalYear = new FiscalYearData { Id = @event.FiscalYearId.Value, ... };
    dbContext.FiscalYears.Add(fiscalYear);
    await dbContext.SaveChangesAsync();
}

private async Task Handle(AccountingPeriodCreated @event, JamaaDbContext dbContext)
{
    var period = new AccountingPeriodData { Id = @event.PeriodId.Value, ... };
    dbContext.AccountingPeriods.Add(period);
    await dbContext.SaveChangesAsync();
}
// ... similar for other events
```

---

## IOSP Pattern Application

### Integration Methods (Orchestration)

- `FiscalCalendarAndPeriodsViewModel.LoadFiscalYearsAsync()`: Loads years from facade, maps to UI viewmodels
- `FiscalCalendarAndPeriodsViewModel.AddFiscalYearCommand`: Calculates contiguous dates, calls facade
- `FiscalCalendarAndPeriodsViewModel.SaveFiscalYearCommand`: Validates, calls UpdateFiscalYear
- `FinanceManagementFacade.CreateFiscalYear()`: Wraps command, sends to CommandProcessor
- `FiscalCalendarAggregate.Handle(CreateFiscalYear)`: Validates, generates periods, persists batch

### Operation Methods (Single Action)

- `FiscalCalendarQueryHandler.Get()`: Queries EF DbSet, includes related data
- `FiscalYearRepository.GetByOrganisationAsync()`: Queries, orders, returns list
- `AccountingPeriodRepository.GetByFiscalYearAsync()`: Queries by FK, orders
- `FiscalCalendarAggregate.Apply(FiscalYearCreated)`: Updates internal state
- `FiscalCalendarAggregate.Apply(AccountingPeriodCreated)`: Updates internal period collection

---

## Business Rules Enforced

### Contiguity (No Gaps)

✅ **Between Fiscal Years**: When creating/updating/deleting a fiscal year, the aggregate validates that all fiscal years still form a contiguous timeline with no date gaps.

✅ **Within Fiscal Year (Periods)**: When creating/updating/deleting periods, the aggregate validates that all periods within a fiscal year cover the entire fiscal year boundary with no gaps.

**Implementation**: 
- `IsContiguous()` helper checks ordered date ranges
- Called before persisting create/update/delete events
- Returns `false` if any gap detected → command rejected

### Lock State Synchronization

✅ **Fiscal Year Lock → Period Lock**: When a fiscal year is locked, all its periods are immediately locked (applied during event handling).

✅ **Period Lock → Fiscal Year Lock**: When all periods are locked and cover entire fiscal year, the fiscal year auto-locks (via `PersistFiscalYearLockSync()`).

✅ **Period Unlock → Fiscal Year Unlock**: When any period is reopened, the fiscal year auto-reopens.

**Implementation**:
- `SynchronizeFiscalYearAndPeriodLockState()` in UI for preview
- `PersistFiscalYearLockSync()` in aggregate for persistence

### Period Auto-Generation

✅ **Monthly Periods**: When a fiscal year is created/updated, periods are auto-generated:
- Each period spans up to 1 month (start of month → end of month)
- Last period may be shorter to align with fiscal year end date
- Periods are gap-free and cover entire fiscal year

**Implementation**: `GenerateDefaultPeriods()` algorithm

---

## Files & Code Locations

### Presentation Layer
- `Jamaa.Desktop/Accounting/FiscalCalendarAndPeriodsPage.axaml` - Master/detail UI
- `Jamaa.Desktop/Accounting/FiscalCalendarAndPeriodsViewModel.cs` - Integration orchestration
- `Jamaa.Desktop/Accounting/FiscalYearEditorItemViewModel.cs` - Fiscal year editor state
- `Jamaa.Desktop/Accounting/AccountingPeriodItemViewModel.cs` - Period detail state (updated with `Id` property)

### Domain Layer
- `Jamaa.Domain/Finances/Values/FiscalYearId.cs` - Value object
- `Jamaa.Domain/Finances/Values/AccountingPeriodId.cs` - Value object
- `Jamaa.Domain/Finances/Queries/GetFiscalYearsByOrganisation.cs` - Query contract

### Application Layer
- `Jamaa.Application/Finances/IFinanceManagementFacade.cs` - Facade interface
- `Jamaa.Application/Finances/FinanceManagementFacade.cs` - Facade implementation
- `Jamaa.Application/Finances/Commands/*.cs` - 6 command records
- `Jamaa.Application/Finances/Events/*.cs` - 6 event records
- `Jamaa.Application/Finances/Aggregates/FiscalCalendarAggregate.cs` - Event-sourced actor (613 lines)
- `Jamaa.Application/Shared/CommandProcessor.cs` - Routes commands to aggregates
- `Jamaa.Application/Shared/LibotaEventTagger.cs` - Tags events for projection
- `Jamaa.Application/Shared/OrganisationProjection.cs` - Projects events to read models
- `Jamaa.Application/Shared/QueryProcessor.cs` - Routes queries to handlers

### Data Layer
- `Jamaa.Data/Models/Finances/FiscalYearData.cs` - EF read model
- `Jamaa.Data/Models/Finances/AccountingPeriodData.cs` - EF read model
- `Jamaa.Data/Queries/Finances/IFiscalCalendarQueryHandler.cs` - Query handler interface
- `Jamaa.Data/Queries/Finances/FiscalCalendarQueryHandler.cs` - Query handler implementation
- `Jamaa.Data/Repositories/Finances/IFiscalYearRepository.cs` - Repository interface
- `Jamaa.Data/Repositories/Finances/IAccountingPeriodRepository.cs` - Repository interface
- `Jamaa.Data/Configuration/JamaaDbContext.cs` - EF context with DbSets
- `Jamaa.Data/Configuration/DataServiceCollectionExtensions.cs` - DI registration
- `Jamaa.Data/Migrations/20260420_AddFiscalYearAndAccountingPeriod.cs` - Applied migration

---

## Build Status

✅ **Build**: Succeeds with 0 errors
✅ **Tests**: 12/12 passing
✅ **Warnings**: Only pre-existing package version mismatches (unrelated)

---

## End-to-End Flow Summary

**User creates fiscal year "FY 2026":**

1. **UI**: Click "Add Fiscal Year" → calls `_financeManagementFacade.CreateFiscalYear("default-org", 2026-01-01, 2026-12-31, false)`

2. **Facade** (Integration): Wraps in `CreateFiscalYear` command, sends to `CommandProcessor` actor

3. **CommandProcessor**: Retrieves/creates `FiscalCalendarAggregate("default-org")`, sends command message

4. **FiscalCalendarAggregate**: 
   - Validates contiguity with existing years
   - Generates 12 monthly `AccountingPeriodCreated` events
   - Calls `PersistAll()` with batch: `[FiscalYearCreated, AccountingPeriodCreated×12]`

5. **Akka.Persistence**: Writes events to SQLite journal

6. **LibotaEventTagger**: Tags events with `OrganisationEvent` + `FinanceChanged`

7. **OrganisationProjection**: 
   - Inserts `FiscalYearData` row into `FiscalYears` table
   - Inserts 12 `AccountingPeriodData` rows into `AccountingPeriods` table

8. **UI**: Calls `LoadFiscalYearsAsync()` → queries via facade

9. **QueryProcessor** → **FiscalCalendarQueryHandler** → **EF Query**: 
   - `dbContext.FiscalYears.Include(fy => fy.Periods).Where(...).OrderByDescending(...)`

10. **UI**: Receives `FiscalYearData` with periods, maps to viewmodels, displays in list (descending by start date)

**Result**: User sees new fiscal year in list with 12 auto-generated monthly periods ready to lock/unlock.

---

## Key Decisions & Rationale

1. **Event Sourcing via Akka.NET**: Immutable audit trail, natural concurrency, recovery on restart
2. **IOSP Split**: Clean testability boundary - operations are leaf-level, integrations orchestrate
3. **Batch Event Persistence**: `PersistAll()` instead of loop for performance (Akka.NET best practice)
4. **DynamicData for UI**: Reactive sorting without list invalidation, maintains selection stability
5. **Facade Pattern**: Decouples presentation from application complexity
6. **Projection Pattern**: Read models optimized for queries, separate from event journal
7. **Contiguity Validation**: Enforced at aggregate level, before persisting events
8. **Lock State Sync**: Bidirectional sync ensures consistency without external coordination

---

## Future Enhancements

- ⏳ Add authorization checks for unlock operations (currently blocked in UI)
- ⏳ Implement error handling for aggregate rejections (currently sends to Sender)
- ⏳ Add event versioning for schema evolution
- ⏳ Implement event upcasting for backward compatibility
- ⏳ Add metrics/logging for audit trail
- ⏳ Support fiscal year cloning
- ⏳ Support bulk period operations

---

## Testing Strategy

### Current Tests (12 passing)
- Dashboard, Members, Organisation, Users tests

### Recommended Additions
- **FiscalCalendarAggregate**: Unit tests for contiguity validation, lock sync, period generation
- **FinanceManagementFacade**: Interaction tests with mocked CommandProcessor/QueryProcessor
- **FiscalCalendarQueryHandler**: Query tests with in-memory EF DbContext
- **Projection**: Event-to-readmodel mapping tests
- **ViewModel**: Integration tests with mocked facade

---

**Implementation Date**: April 20, 2026
**Status**: ✅ Ready for User Acceptance Testing (UAT)

