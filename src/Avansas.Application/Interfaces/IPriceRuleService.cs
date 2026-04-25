using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IPriceRuleService
{
    Task<List<PriceRuleDto>> GetAllRulesAsync(bool? isActive = null);
    Task<PriceRuleDto?> GetRuleByIdAsync(int id);
    Task<int> CreateRuleAsync(CreatePriceRuleDto dto);
    Task UpdateRuleAsync(PriceRuleDto dto);
    Task DeleteRuleAsync(int id);
    Task ToggleActiveAsync(int id);
    Task<decimal> CalculatePriceAsync(int productId, decimal basePrice, int quantity = 1, string? customerGroup = null);
}
