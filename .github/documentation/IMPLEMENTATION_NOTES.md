# Event-Confirmed Success Messages Implementation

## Summary
Implemented a new pattern where success messages are only shown after the corresponding server-side event is confirmed, rather than immediately after sending the command. This ensures users only see success messages when operations are actually persisted in the database.

## Changes Made

### 1. New Extension Methods
**File**: `/Jamaa.Desktop/Services/Notifications/CommandOperationTracker.cs`

Created `CommandOperationExtensions` with two overloads of `TrackOperationAsync<TEvent>()`:

#### Overload 1: Simple event confirmation
```csharp
TrackOperationAsync<TEvent>(
    sendCommand: Func<Task>,
    confirmationObservable: IObservable<TEvent>,
    timeout: TimeSpan,
    operationName: string,
    successAction: string,
    subject?: string)
```

#### Overload 2: Event confirmation with predicate matching
```csharp
TrackOperationAsync<TEvent>(
    sendCommand: Func<Task>,
    confirmationObservable: IObservable<TEvent>,
    matcherPredicate?: Func<TEvent, bool>,
    timeout: TimeSpan,
    operationName: string,
    successAction: string,
    subject?: string)
```

**Behavior**:
- Shows "Requested ..." message immediately (in-flight)
- Waits for observable to emit a matching event (with 10-second default timeout)
- Shows "Saved/Created/Deleted ..." message only after confirmation
- Returns `true` if confirmed, `false` if timeout or error

### 2. Updated ViewModels

#### MemberProfileViewModel
**File**: `/Jamaa.Desktop/Members/Pages/MemberProfileViewModel.cs`

**Changed**: `Save()` command
- **Before**: Showed success message immediately after `UpdateMember()` call
- **After**: Uses `TrackOperationAsync()` to wait for `MemberUpdated` event confirmation
- Only updates UI state after event is confirmed

#### MemberListViewModel  
**File**: `/Jamaa.Desktop/Members/Components/MemberListViewModel.cs`

**Changed 1**: `RegisterMember()` command
- Tracks registration with `CurrentMembers` observable
- Matches by first name and last name

**Changed 2**: `EndRegistration()` command
- Tracks registration ending with `MemberUpdated` observable
- Matches by member ID

### 3. Documentation
**File**: `/Jamaa.Desktop/Services/Notifications/EVENT_CONFIRMED_MESSAGES.md`

Created comprehensive guide covering:
- Pattern overview and rationale
- Usage examples
- Parameter descriptions
- Observable selection guidelines
- Migration instructions
- Best practices

## Architecture

### Integration vs Operation Classification

The new extension methods follow the IOSP (Integration/Operation Split Pattern):

**Integration**: `TrackOperationAsync()` 
- Orchestrates the command → event workflow
- Delegates to operations for specific tasks
- Reads like workflow steps

**Operations**:
- `ShowRequestedMessage()` - displays initial message
- `ShowSuccessMessage()` - displays confirmation message
- `ShowErrorMessage()` - displays error message
- `WaitForConfirmationAsync()` - waits for event with timeout

## User Experience Flow

### Before (Immediate Success)
```
User clicks "Save" 
  ↓
ShowNotification("Saved successfully")  ← Shown immediately
  ↓
Command sent to actor (may still fail)
  ↓
Event eventually persisted
```

### After (Event-Confirmed Success)
```
User clicks "Save"
  ↓
ShowNotification("Requested ...")  ← Shown immediately
  ↓
Command sent to actor
  ↓
Event persisted in database
  ↓
Observable fires
  ↓
ShowNotification("Saved successfully")  ← Shown only now
```

## Key Benefits

1. **Accuracy**: Success messages only appear after actual persistence
2. **User Trust**: No false success notifications
3. **Error Visibility**: Timeouts and failures are properly reported
4. **Consistency**: Standard pattern across all operations
5. **Observability**: Easier to trace operation lifecycle

## Timeout Handling

Default timeout: **10 seconds**

If no confirmation event received within timeout:
- No success message is shown
- User sees no feedback (operation remains in-flight state)
- Can be configured per-operation if needed

## Observable Selection Guide

| Operation | Observable | Notes |
|-----------|-----------|-------|
| Create | `CurrentMembers` or `.Insertions` | Fires when added to collection |
| Update | `.MemberUpdated` or `.Updates` | Fires when record modified |
| Delete | `.MemberDeleted` or `.Deletions` | Fires when record removed |

## Testing Considerations

When testing ViewModels that use this pattern:

1. Mock the facade's observable streams
2. Simulate event emission after command execution
3. Verify success message only appears after observable fires
4. Test timeout scenarios (no event within 10 seconds)
5. Test error handling (exception during command send)

Example:
```csharp
var memberUpdatedSubject = new Subject<MemberData>();
facadeMock.MemberUpdated.Returns(memberUpdatedSubject);

// Trigger command
viewModel.SaveCommand.Execute(null);

// Wait for "Requested" message
await Task.Delay(100);
AssertRequestedMessageShown();

// Emit event
memberUpdatedSubject.OnNext(updatedMember);

// Verify success message
AssertSuccessMessageShown();
```

## Migration Path for Existing Code

### Pattern 1: Simple Update → Confirmation
```csharp
// Old
await facade.UpdateItem(request);
notificationService.Show("Success", "Item updated.", NotificationType.Success);

// New
var isConfirmed = await notificationService.TrackOperationAsync(
    sendCommand: () => facade.UpdateItem(request),
    confirmationObservable: facade.ItemUpdated,
    matcherPredicate: item => item.Id == request.ItemId,
    timeout: TimeSpan.FromSeconds(10),
    operationName: "Item",
    successAction: "Saved");
```

### Pattern 2: Create → Confirmation
```csharp
// Old
await facade.CreateItem(request);
notificationService.Show("Success", "Item created.", NotificationType.Success);

// New
var isConfirmed = await notificationService.TrackOperationAsync(
    sendCommand: () => facade.CreateItem(request),
    confirmationObservable: facade.CurrentItems,
    timeout: TimeSpan.FromSeconds(10),
    operationName: "Item",
    successAction: "Created",
    subject: request.ItemName);
```

## Files Changed

1. **Created**:
   - `/Jamaa.Desktop/Services/Notifications/CommandOperationTracker.cs`
   - `/Jamaa.Desktop/Services/Notifications/EVENT_CONFIRMED_MESSAGES.md`

2. **Modified**:
   - `/Jamaa.Desktop/Members/Pages/MemberProfileViewModel.cs`
   - `/Jamaa.Desktop/Members/Components/MemberListViewModel.cs`

## Compatibility

- **Reactive Extensions**: Uses `System.Reactive` operators
- **C# 14**: Uses modern C# features (records, file-scoped namespaces)
- **Avalonia**: Compatible with existing notification system
- **Akka.NET**: Designed for fire-and-forget command pattern

## Next Steps

1. Apply pattern to other critical ViewModels:
   - Accounting operations
   - User management
   - Group management
   - Any other facade-based operations

2. Update tests to verify:
   - "Requested" message appears before confirmation
   - "Saved/Created/Deleted" only appears after event
   - Timeouts are handled gracefully
   - Errors show appropriate messages

3. Consider adding progress indicators:
   - Could show a spinner/loading state while waiting for confirmation
   - UI feedback that operation is in-flight but not yet confirmed

