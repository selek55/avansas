using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class SupportTicket : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public int? OrderId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Order? Order { get; set; }
    public List<TicketMessage> Messages { get; set; } = new();
}
