namespace Avansas.Domain.Entities;

public class ReturnItem : BaseEntity
{
    public int ReturnRequestId { get; set; }
    public int OrderItemId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }

    public ReturnRequest ReturnRequest { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}
