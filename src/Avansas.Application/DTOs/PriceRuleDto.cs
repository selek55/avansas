using Avansas.Domain.Enums;

namespace Avansas.Application.DTOs;

public class PriceRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriceRuleType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public bool IsPercentage { get; set; }
    public int? MinQuantity { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? CustomerGroup { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePriceRuleDto
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
}
