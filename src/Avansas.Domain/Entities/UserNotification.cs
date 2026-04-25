using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class UserNotification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public string? Link { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
