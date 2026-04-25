using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    public NotificationService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count = 20)
    {
        var notifications = await _uow.UserNotifications.Query()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _uow.UserNotifications.CountAsync(
            n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? link = null)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            Link = link
        };
        await _uow.UserNotifications.AddAsync(notification);
        await _uow.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _uow.UserNotifications.GetByIdAsync(notificationId);
        if (notification == null || notification.IsDeleted) return;

        notification.IsRead = true;
        _uow.UserNotifications.Update(notification);
        await _uow.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unread = await _uow.UserNotifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            _uow.UserNotifications.Update(n);
        }
        await _uow.SaveChangesAsync();
    }

    private static NotificationDto MapToDto(UserNotification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        Link = n.Link,
        CreatedAt = n.CreatedAt
    };
}
