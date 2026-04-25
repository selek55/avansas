namespace Avansas.Domain.Entities;

public class StockNotification : BaseEntity
{
    public int ProductId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsNotified { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public Product Product { get; set; } = null!;
}
