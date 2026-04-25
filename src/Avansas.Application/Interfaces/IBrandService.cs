using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllBrandsAsync(bool? isActive = null);
    Task<BrandDto?> GetBrandByIdAsync(int id);
    Task<int> CreateBrandAsync(CreateBrandDto dto);
    Task UpdateBrandAsync(UpdateBrandDto dto);
    Task DeleteBrandAsync(int id);
    Task ToggleActiveAsync(int id);
}
