namespace Avansas.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int MinStockAlert { get; set; } = 5;
    public string? MainImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsNewProduct { get; set; } = false;
    public decimal Weight { get; set; }
    public string? Unit { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal TaxRate { get; set; } = 18;
    public int ViewCount { get; set; } = 0;

    public Category Category { get; set; } = null!;
    public Brand? Brand { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    public decimal GetEffectivePrice() => DiscountedPrice.HasValue && DiscountedPrice.Value < Price
        ? DiscountedPrice.Value : Price;

    public decimal GetDiscountPercentage() => DiscountedPrice.HasValue && DiscountedPrice.Value < Price
        ? Math.Round((1 - DiscountedPrice.Value / Price) * 100) : 0;
}
