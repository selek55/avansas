using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<List<CategoryDto>> GetRootCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
    Task<List<CategoryDto>> GetSubCategoriesAsync(int parentId);
    Task<int> CreateCategoryAsync(CreateCategoryDto dto);
    Task UpdateCategoryAsync(UpdateCategoryDto dto);
    Task DeleteCategoryAsync(int id);
}
