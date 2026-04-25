using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class PriceRuleService : IPriceRuleService
{
    private readonly IUnitOfWork _uow;

    public PriceRuleService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<PriceRuleDto>> GetAllRulesAsync(bool? isActive = null)
    {
        var query = _uow.PriceRules.Query()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Include(r => r.Brand)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var rules = await query.OrderByDescending(r => r.Priority).ThenBy(r => r.Name).ToListAsync();
        return rules.Select(MapToDto).ToList();
    }

    public async Task<PriceRuleDto?> GetRuleByIdAsync(int id)
    {
        var rule = await _uow.PriceRules.Query()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Include(r => r.Brand)
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule == null ? null : MapToDto(rule);
    }

    public async Task<int> CreateRuleAsync(CreatePriceRuleDto dto)
    {
        var rule = new PriceRule
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            DiscountValue = dto.DiscountValue,
            IsPercentage = dto.IsPercentage,
            MinQuantity = dto.MinQuantity,
            ProductId = dto.ProductId,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            CustomerGroup = dto.CustomerGroup,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            IsActive = dto.IsActive
        };

        await _uow.PriceRules.AddAsync(rule);
        await _uow.SaveChangesAsync();
        return rule.Id;
    }

    public async Task UpdateRuleAsync(PriceRuleDto dto)
    {
        var rule = await _uow.PriceRules.GetByIdAsync(dto.Id)
            ?? throw new Exception("Fiyat kuralı bulunamadı");

        rule.Name = dto.Name;
        rule.Description = dto.Description;
        rule.Type = dto.Type;
        rule.DiscountValue = dto.DiscountValue;
        rule.IsPercentage = dto.IsPercentage;
        rule.MinQuantity = dto.MinQuantity;
        rule.ProductId = dto.ProductId;
        rule.CategoryId = dto.CategoryId;
        rule.BrandId = dto.BrandId;
        rule.CustomerGroup = dto.CustomerGroup;
        rule.StartDate = dto.StartDate;
        rule.EndDate = dto.EndDate;
        rule.Priority = dto.Priority;
        rule.IsActive = dto.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        _uow.PriceRules.Update(rule);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteRuleAsync(int id)
    {
        var rule = await _uow.PriceRules.GetByIdAsync(id)
            ?? throw new Exception("Fiyat kuralı bulunamadı");

        _uow.PriceRules.SoftDelete(rule);
        await _uow.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(int id)
    {
        var rule = await _uow.PriceRules.GetByIdAsync(id)
            ?? throw new Exception("Fiyat kuralı bulunamadı");

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        _uow.PriceRules.Update(rule);
        await _uow.SaveChangesAsync();
    }

    public async Task<decimal> CalculatePriceAsync(int productId, decimal basePrice, int quantity = 1, string? customerGroup = null)
    {
        var now = DateTime.UtcNow;

        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return basePrice;

        var rules = await _uow.PriceRules.Query()
            .Where(r => r.IsActive)
            .Where(r => !r.StartDate.HasValue || r.StartDate <= now)
            .Where(r => !r.EndDate.HasValue || r.EndDate >= now)
            .Where(r => !r.MinQuantity.HasValue || r.MinQuantity <= quantity)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        // Filter applicable rules
        var applicable = rules.Where(r =>
            (r.ProductId == null && r.CategoryId == null && r.BrandId == null && r.CustomerGroup == null) ||
            (r.ProductId != null && r.ProductId == productId) ||
            (r.CategoryId != null && r.CategoryId == product.CategoryId) ||
            (r.BrandId != null && r.BrandId == product.BrandId) ||
            (r.CustomerGroup != null && r.CustomerGroup == customerGroup)
        ).ToList();

        if (!applicable.Any()) return basePrice;

        // Apply highest priority rule
        var bestRule = applicable.First();
        decimal discount = bestRule.IsPercentage
            ? basePrice * bestRule.DiscountValue / 100m
            : bestRule.DiscountValue;

        var finalPrice = basePrice - discount;
        return finalPrice < 0 ? 0 : Math.Round(finalPrice, 2);
    }

    private static PriceRuleDto MapToDto(PriceRule r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Type = r.Type,
        DiscountValue = r.DiscountValue,
        IsPercentage = r.IsPercentage,
        MinQuantity = r.MinQuantity,
        ProductId = r.ProductId,
        ProductName = r.Product?.Name,
        CategoryId = r.CategoryId,
        CategoryName = r.Category?.Name,
        BrandId = r.BrandId,
        BrandName = r.Brand?.Name,
        CustomerGroup = r.CustomerGroup,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        Priority = r.Priority,
        IsActive = r.IsActive
    };
}
