using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductFilterDto filter);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto?> GetProductBySlugAsync(string slug);
    Task<List<ProductListDto>> GetFeaturedProductsAsync(int count = 8);
    Task<List<ProductListDto>> GetNewProductsAsync(int count = 8);
    Task<List<ProductListDto>> GetProductsByCategoryAsync(int categoryId, int count = 20);
    Task<List<ProductListDto>> GetRelatedProductsAsync(int productId, int count = 6);
    Task<int> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(UpdateProductDto dto);
    Task DeleteProductAsync(int id);
    Task UpdateStockAsync(int productId, int quantity);
    Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null);
    Task<List<ProductImageDto>> GetProductImagesAsync(int productId);
    Task<ProductImageDto> AddProductImageAsync(int productId, string imageUrl, bool isMain = false);
    Task DeleteProductImageAsync(int imageId);
    Task SetMainImageAsync(int productId, int imageId);
    Task ReorderImagesAsync(int productId, List<int> orderedImageIds);
    Task<List<ProductListDto>> SearchSuggestionsAsync(string term, int count = 6);

    // Variant management
    Task<List<ProductVariantDto>> GetProductVariantsAsync(int productId);
    Task<int> AddVariantAsync(int productId, ProductVariantDto dto);
    Task UpdateVariantAsync(ProductVariantDto dto);
    Task DeleteVariantAsync(int variantId);
}
