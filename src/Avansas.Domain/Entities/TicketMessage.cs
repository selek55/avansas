namespace Avansas.Domain.Entities;

public class TicketMessage : BaseEntity
{
    public int TicketId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAdminReply { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}
