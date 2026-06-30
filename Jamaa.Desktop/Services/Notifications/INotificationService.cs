using System;

namespace Jamaa.Desktop.Services.Notifications;

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

public interface INotificationService
{
    void Show(string title, string message, NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null, Action? onClick = null, Action? onClose = null);

    void ShowLink(string title, string message, string linkText, NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null, Action? onLinkClick = null, Action? onClose = null);
}
