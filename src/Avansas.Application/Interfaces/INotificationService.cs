using Avansas.Application.DTOs;
using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count = 20);
    Task<int> GetUnreadCountAsync(string userId);
    Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? link = null);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
}
