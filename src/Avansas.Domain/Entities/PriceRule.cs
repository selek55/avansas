using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class PriceRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriceRuleType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public bool IsPercentage { get; set; }
    public int? MinQuantity { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? CustomerGroup { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;

    public Product? Product { get; set; }
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
}
