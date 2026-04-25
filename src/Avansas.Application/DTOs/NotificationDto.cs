using Avansas.Domain.Enums;

namespace Avansas.Application.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TypeIcon => Type switch
    {
        NotificationType.OrderStatus => "fas fa-shopping-bag",
        NotificationType.StockAlert => "fas fa-box",
        NotificationType.Campaign => "fas fa-tags",
        NotificationType.Review => "fas fa-star",
        NotificationType.TicketReply => "fas fa-headset",
        NotificationType.ReturnUpdate => "fas fa-undo",
        NotificationType.LoyaltyPoints => "fas fa-coins",
        NotificationType.System => "fas fa-bell",
        _ => "fas fa-bell"
    };
}
