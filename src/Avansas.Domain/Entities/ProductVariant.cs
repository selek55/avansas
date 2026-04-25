namespace Avansas.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;        // "Renk", "Boyut"
    public string Value { get; set; } = string.Empty;       // "Kırmızı", "A4"
    public string? SKU { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public Product Product { get; set; } = null!;
}
