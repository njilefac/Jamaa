using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Jamaa.Desktop.Services.Notifications;

namespace Jamaa.Desktop.Services.Notifications;

/// <summary>
/// Extension methods for managing command operation lifecycle:
/// 1. "Requested ..." shown immediately when command is sent
/// 2. In-flight callback toggled while waiting for confirmation
/// 3. "Created/Saved/Deleted ..." shown when event is confirmed via observable
/// </summary>
public static class CommandOperationExtensions
{
    /// <summary>
    /// Tracks an operation by showing "Requested" immediately and waiting for event confirmation.
    /// </summary>
    public static async Task<bool> TrackOperationAsync<TEvent>(
        this INotificationService notificationService,
        Func<Task> sendCommand,
        IObservable<TEvent> confirmationObservable,
        TimeSpan timeout,
        string operationName,
        string successAction,
        string? subject = null,
        Action<bool>? inFlightChanged = null)
    {
        // INTEGRATION: Orchestrates command sending, in-flight state and user feedback
        ShowRequestedMessage(notificationService, operationName, subject);
        inFlightChanged?.Invoke(true);

        try
        {
            await sendCommand();
            var isConfirmed = await WaitForConfirmationAsync(confirmationObservable, timeout);

            if (isConfirmed)
            {
                ShowSuccessMessage(notificationService, operationName, successAction, subject);
            }

            return isConfirmed;
        }
        catch (TimeoutException)
        {
            ShowTimeoutMessage(notificationService, operationName, subject);
            return false;
        }
        catch
        {
            ShowErrorMessage(notificationService, successAction, subject);
            return false;
        }
        finally
        {
            inFlightChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Overload for operations where you can filter by a predicate (e.g., matching by ID).
    /// </summary>
    public static async Task<bool> TrackOperationAsync<TEvent>(
        this INotificationService notificationService,
        Func<Task> sendCommand,
        IObservable<TEvent> confirmationObservable,
        Func<TEvent, bool>? matcherPredicate,
        TimeSpan timeout,
        string operationName,
        string successAction,
        string? subject = null,
        Action<bool>? inFlightChanged = null)
    {
        // INTEGRATION: Orchestrates command sending, matched confirmation and UI lifecycle state
        ShowRequestedMessage(notificationService, operationName, subject);
        inFlightChanged?.Invoke(true);

        try
        {
            await sendCommand();

            var observable = matcherPredicate != null
                ? confirmationObservable.Where(matcherPredicate)
                : confirmationObservable;

            var isConfirmed = await WaitForConfirmationAsync(observable, timeout);

            if (isConfirmed)
            {
                ShowSuccessMessage(notificationService, operationName, successAction, subject);
            }

            return isConfirmed;
        }
        catch (TimeoutException)
        {
            ShowTimeoutMessage(notificationService, operationName, subject);
            return false;
        }
        catch
        {
            ShowErrorMessage(notificationService, successAction, subject);
            return false;
        }
        finally
        {
            inFlightChanged?.Invoke(false);
        }
    }

    // OPERATION: Displays "Requested ..." message
    private static void ShowRequestedMessage(
        INotificationService notificationService,
        string operationName,
        string? subject)
    {
        var message = subject != null
            ? $"Requested {subject}..."
            : $"Requested {operationName}...";
        notificationService.Show(operationName, message, NotificationType.Information);
    }

    // OPERATION: Displays "Created/Saved/Deleted ..." message
    private static void ShowSuccessMessage(
        INotificationService notificationService,
        string operationName,
        string successAction,
        string? subject)
    {
        var message = subject != null
            ? $"{successAction} {subject} successfully."
            : $"{successAction} successfully.";
        notificationService.Show(operationName, message, NotificationType.Success);
    }

    // OPERATION: Displays timeout message
    private static void ShowTimeoutMessage(
        INotificationService notificationService,
        string operationName,
        string? subject)
    {
        var message = subject != null
            ? $"Timed out while waiting to confirm {subject}. Please retry."
            : $"Timed out while waiting to confirm {operationName}. Please retry.";
        notificationService.Show("Timeout", message, NotificationType.Warning);
    }

    // OPERATION: Displays error message
    private static void ShowErrorMessage(
        INotificationService notificationService,
        string successAction,
        string? subject)
    {
        var message = subject != null
            ? $"Failed to {successAction.ToLower()} {subject}."
            : $"Failed to {successAction.ToLower()}.";
        notificationService.Show("Error", message, NotificationType.Error);
    }

    // OPERATION: Waits for observable to emit a value within timeout
    private static async Task<bool> WaitForConfirmationAsync<TEvent>(
        IObservable<TEvent> observable,
        TimeSpan timeout)
    {
        return await observable
            .Timeout(timeout)
            .Take(1)
            .Select(_ => true)
            .FirstAsync();
    }
}

