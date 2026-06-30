using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;

namespace Jamaa.Desktop.Services.Notifications;

public class AvaloniaNotificationService : INotificationService
{
    private INotificationManager? _notificationManager;

    public void Show(string title, string message, NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null, Action? onClick = null, Action? onClose = null)
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

    public void ShowLink(string title, string message, string linkText, NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null, Action? onLinkClick = null, Action? onClose = null)
    {
        if (_notificationManager == null)
            return;

        var avaloniaType = ToAvaloniaNotificationType(type);
        if (_notificationManager is not WindowNotificationManager windowNotificationManager)
        {
            Show(title, $"{message} {linkText}", type, expiration, onLinkClick, onClose);
            return;
        }

        Control? content = null;
        content = CreateLinkContent(title, message, linkText, () =>
        {
            onLinkClick?.Invoke();
            if (content != null)
            {
                windowNotificationManager.Close(content);
            }
        });

        windowNotificationManager.Show(content,
            avaloniaType,
            expiration,
            null,
            onClose);
    }

    public void SetNotificationManager(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    private static Avalonia.Controls.Notifications.NotificationType ToAvaloniaNotificationType(NotificationType type)
    {
        return type switch
        {
            NotificationType.Information => Avalonia.Controls.Notifications.NotificationType.Information,
            NotificationType.Success => Avalonia.Controls.Notifications.NotificationType.Success,
            NotificationType.Warning => Avalonia.Controls.Notifications.NotificationType.Warning,
            NotificationType.Error => Avalonia.Controls.Notifications.NotificationType.Error,
            _ => Avalonia.Controls.Notifications.NotificationType.Information
        };
    }

    private static Control CreateLinkContent(string title, string message, string linkText, Action? onLinkClick)
    {
        var link = new HyperlinkButton
        {
            Content = linkText,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Avalonia.Thickness(0, 2, 0, 0)
        };
        link.Click += (_, _) => onLinkClick?.Invoke();

        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                link
            }
        };
    }
}
