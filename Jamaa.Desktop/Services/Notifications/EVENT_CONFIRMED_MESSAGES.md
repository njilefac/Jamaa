## Event-Confirmed Success Messages Pattern

### Overview

This document explains the event-confirmed success message pattern used in Jamaa Desktop to provide accurate,
event-based feedback to users.

### Problem Statement

Previously, success messages were shown immediately after sending a command to the actor system, without waiting for the
actual event to be persisted in the database. This could mislead users into thinking an operation succeeded when it
might still fail.

### Solution Pattern

The solution uses a two-phase notification approach:

1. **Phase 1 (In-Flight)**: Show "Requested ..." message immediately when command is sent
2. **Phase 2 (Confirmed)**: Replace with "Created/Saved/Deleted ..." message only after event is confirmed by the
   database observer

### Usage

#### Step 1: Inject INotificationService

```csharp
public class MyViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly IMyFacade _facade;
    
    public MyViewModel(IMyFacade facade, INotificationService notificationService)
    {
        _facade = facade;
        _notificationService = notificationService;
    }
}
```

#### Step 2: Call TrackOperationAsync

Use the extension method in `CommandOperationExtensions` to track your operation:

```csharp
[RelayCommand]
private async Task UpdateItem()
{
    var request = new ItemUpdateRequest { /* ... */ };
    var subject = $"{request.ItemName}"; // Optional: used in message detail
    
    var isConfirmed = await _notificationService.TrackOperationAsync(
        sendCommand: () => _facade.UpdateItem(request),              // Fire-and-forget command
        confirmationObservable: _facade.ItemUpdated,                 // Event confirmation stream
        matcherPredicate: item => item.Id == request.ItemId,        // Optional: filter by predicate
        timeout: TimeSpan.FromSeconds(10),                          // Max wait time
        operationName: "Item",                                       // Display name (for title)
        successAction: "Saved",                                      // Action verb (Created/Updated/Deleted)
        subject: subject);                                           // Optional: detail subject
    
    if (isConfirmed)
    {
        // Only execute post-confirmation logic here
        // e.g., UI state cleanup, navigation, etc.
    }
}
```

### Message Examples

#### Example 1: Create Member

```
In-Flight:    "Requested John Smith..."
Confirmed:    "Registered John Smith successfully."
```

#### Example 2: Update Member

```
In-Flight:    "Requested Jane Doe..."
Confirmed:    "Saved Jane Doe successfully."
```

#### Example 3: End Registration

```
In-Flight:    "Requested Member..."
Confirmed:    "Registration ended for Member successfully."
```

### Key Parameters

| Parameter                | Type             | Purpose                                             | Example                             |
|--------------------------|------------------|-----------------------------------------------------|-------------------------------------|
| `sendCommand`            | `Func<Task>`     | Function that sends the command (fire-and-forget)   | `() => facade.CreateItem(req)`      |
| `confirmationObservable` | `IObservable<T>` | Observable that fires when event is confirmed       | `facade.ItemCreated`                |
| `matcherPredicate`       | `Func<T, bool>`  | Optional: filter observable to match specific event | `item => item.Id == req.Id`         |
| `timeout`                | `TimeSpan`       | Maximum time to wait for confirmation               | `TimeSpan.FromSeconds(10)`          |
| `operationName`          | `string`         | Display name shown in notification title            | `"Member"`, `"Account"`             |
| `successAction`          | `string`         | Action verb used in final message                   | `"Created"`, `"Saved"`, `"Deleted"` |
| `subject`                | `string?`        | Optional: detail subject in message                 | `"John Smith"`, `"USD"`             |

### Observable Options

When choosing which observable to use, follow these guidelines:

- **For Create Operations**: Use `facade.CurrentMembers` or similar "added to list" observables
- **For Update Operations**: Use `facade.ItemUpdated` which fires when item changes
- **For Delete Operations**: Use `facade.ItemDeleted`
- **For Complex Changes**: Use filtered `Updates` observable with a matcher predicate

### Integration vs Operation Classification

This pattern is an **INTEGRATION** method because it:

- Orchestrates a workflow (send command → wait for event → show message)
- Delegates actual operations to facades and notification service
- Reads like a sequence of workflow steps

### Error Handling

The `TrackOperationAsync` method automatically handles errors:

- If an exception occurs during command sending: Shows error notification
- If confirmation times out: Shows error notification (no success message)
- The return value indicates success (true) or failure (false)

### Best Practices

1. **Always use event observables**: Don't show success messages without event confirmation
2. **Be specific with matchers**: Use predicates to match the exact resource being modified
3. **Keep timeouts reasonable**: 10 seconds is typical; adjust based on expected latency
4. **Show UI confirmation**: Only update UI state after confirmation (if needed)
5. **Provide meaningful subjects**: Help users identify which item was affected

### Migration Guide

Existing code like this:

```csharp
await _facade.UpdateMember(request);
_notificationService.Show("Success", "Member updated successfully.", NotificationType.Success);
```

Should be updated to:

```csharp
var isConfirmed = await _notificationService.TrackOperationAsync(
    sendCommand: () => _facade.UpdateMember(request),
    confirmationObservable: _facade.MemberUpdated,
    matcherPredicate: m => m.Id == request.MemberId,
    timeout: TimeSpan.FromSeconds(10),
    operationName: "Member",
    successAction: "Saved");
```

### Reference Implementation

See `MemberProfileViewModel.Save()` for a complete example of the pattern in use.

