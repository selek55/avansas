namespace Avansas.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal TotalPrice => UnitPrice * Quantity;
}
