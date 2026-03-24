using System;
using Avalonia.Controls.Notifications;

namespace Jamaa.Desktop.Services.Notifications;

public class AvaloniaNotificationService : INotificationService
{
    private INotificationManager? _notificationManager;

    public void SetNotificationManager(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    public void Show(string title, string message, NotificationType type = NotificationType.Information, TimeSpan? expiration = null, Action? onClick = null, Action? onClose = null)
    {
        if (_notificationManager == null)
            return;

        var avaloniaType = type switch
        {
            NotificationType.Information => Avalonia.Controls.Notifications.NotificationType.Information,
            NotificationType.Success => Avalonia.Controls.Notifications.NotificationType.Success,
            NotificationType.Warning => Avalonia.Controls.Notifications.NotificationType.Warning,
            NotificationType.Error => Avalonia.Controls.Notifications.NotificationType.Error,
            _ => Avalonia.Controls.Notifications.NotificationType.Information
        };

        _notificationManager.Show(new Notification(title, message, avaloniaType, expiration, onClick, onClose));
    }
}
