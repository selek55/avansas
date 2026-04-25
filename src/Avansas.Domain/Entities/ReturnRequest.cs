using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class ReturnRequest : BaseEntity
{
    public int OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public decimal RefundAmount { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public Order Order { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}
