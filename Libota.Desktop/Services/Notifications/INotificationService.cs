using System;

namespace Libota.Desktop.Services.Notifications;

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

public interface INotificationService
{
    void Show(string title, string message, NotificationType type = NotificationType.Information, TimeSpan? expiration = null, Action? onClick = null, Action? onClose = null);
}
