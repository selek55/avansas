using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;

    public ProductService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ProductListDto>> GetProductsAsync(ProductFilterDto filter)
    {
        var query = _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Reviews)
            .Where(p => !p.IsDeleted);

        if (filter.IsActive.HasValue) query = query.Where(p => p.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(p => p.Name.Contains(filter.SearchTerm) || p.SKU.Contains(filter.SearchTerm));
        if (filter.CategoryId.HasValue) query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        if (filter.BrandId.HasValue) query = query.Where(p => p.BrandId == filter.BrandId.Value);
        if (filter.BrandIds != null && filter.BrandIds.Any()) query = query.Where(p => p.BrandId.HasValue && filter.BrandIds.Contains(p.BrandId.Value));
        if (filter.MinPrice.HasValue) query = query.Where(p => p.Price >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue) query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        if (filter.MinRating.HasValue) query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= filter.MinRating.Value);
        if (filter.InStock == true) query = query.Where(p => p.StockQuantity > 0);

        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "bestseller" => query.OrderByDescending(p => p.OrderItems.Count),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ProductListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted);

        if (product != null) { product.ViewCount++; await _uow.SaveChangesAsync(); }
        return product == null ? null : MapToDto(product);
    }

    public async Task<List<ProductListDto>> GetFeaturedProductsAsync(int count = 8)
    {
        var products = await _uow.Products.Query()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
            .Where(p => p.IsFeatured && p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync();
        return products.Select(MapToListDto).ToList();
    }

    public async Task<List<ProductListDto>> GetNewProductsAsync(int count = 8)
    {
        var products = await _uow.Products.Query()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
            .Where(p => p.IsNewProduct && p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync();
        return products.Select(MapToListDto).ToList();
    }

    public async Task<List<ProductListDto>> GetProductsByCategoryAsync(int categoryId, int count = 20)
    {
        var products = await _uow.Products.Query()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
            .Where(p => p.CategoryId == categoryId && p.IsActive && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync();
        return products.Select(MapToListDto).ToList();
    }

    public async Task<List<ProductListDto>> GetRelatedProductsAsync(int productId, int count = 6)
    {
        var product = await _uow.Products.GetByIdAsync(productId);
        if (product == null) return new();

        var products = await _uow.Products.Query()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsActive && !p.IsDeleted)
            .Take(count).ToListAsync();
        return products.Select(MapToListDto).ToList();
    }

    public async Task<List<ProductListDto>> SearchSuggestionsAsync(string term, int count = 6)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2) return new();
        var products = await _uow.Products.Query()
            .Include(p => p.Brand)
            .Where(p => p.IsActive && !p.IsDeleted &&
                (p.Name.Contains(term) || p.SKU.Contains(term)))
            .OrderByDescending(p => p.IsFeatured)
            .Take(count).ToListAsync();
        return products.Select(MapToListDto).ToList();
    }

    public async Task<int> CreateProductAsync(CreateProductDto dto)
    {
        var slug = !string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Slug) : GenerateSlug(dto.Name);
        var product = new Product
        {
            Name = dto.Name, Slug = slug, ShortDescription = dto.ShortDescription,
            Description = dto.Description, SKU = dto.SKU, Barcode = dto.Barcode,
            Price = dto.Price, DiscountedPrice = dto.DiscountedPrice, CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity, MinStockAlert = dto.MinStockAlert,
            IsActive = dto.IsActive, IsFeatured = dto.IsFeatured, IsNewProduct = dto.IsNewProduct,
            Weight = dto.Weight, Unit = dto.Unit, CategoryId = dto.CategoryId, BrandId = dto.BrandId,
            TaxRate = dto.TaxRate, MainImageUrl = dto.MainImageUrl
        };
        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateProductAsync(UpdateProductDto dto)
    {
        var product = await _uow.Products.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Product {dto.Id} not found");

        if (!string.IsNullOrWhiteSpace(dto.Slug)) product.Slug = GenerateSlug(dto.Slug);
        product.Name = dto.Name; product.ShortDescription = dto.ShortDescription;
        product.Description = dto.Description; product.SKU = dto.SKU; product.Barcode = dto.Barcode;
        product.Price = dto.Price; product.DiscountedPrice = dto.DiscountedPrice; product.CostPrice = dto.CostPrice;
        product.StockQuantity = dto.StockQuantity; product.MinStockAlert = dto.MinStockAlert;
        product.IsActive = dto.IsActive; product.IsFeatured = dto.IsFeatured; product.IsNewProduct = dto.IsNewProduct;
        product.Weight = dto.Weight; product.Unit = dto.Unit; product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId; product.TaxRate = dto.TaxRate; product.MainImageUrl = dto.MainImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _uow.Products.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found");
        _uow.Products.SoftDelete(product);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateStockAsync(int productId, int quantity)
    {
        var product = await _uow.Products.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found");
        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null)
    {
        var query = _uow.Products.Query().Where(p => p.Slug == slug && !p.IsDeleted);
        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLower()
            .Replace(" ", "-").Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ö", "o").Replace("ç", "c").Replace("ı", "i").Replace("İ", "i");
        return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
    }

    private static ProductListDto MapToListDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Slug = p.Slug, Price = p.Price,
        DiscountedPrice = p.DiscountedPrice, EffectivePrice = p.GetEffectivePrice(),
        DiscountPercentage = p.GetDiscountPercentage(), MainImageUrl = p.MainImageUrl,
        CategoryName = p.Category?.Name, BrandName = p.Brand?.Name,
        StockQuantity = p.StockQuantity, IsActive = p.IsActive, IsFeatured = p.IsFeatured,
        AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
        ReviewCount = p.Reviews.Count, CreatedAt = p.CreatedAt
    };

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Slug = p.Slug, ShortDescription = p.ShortDescription,
        Description = p.Description, SKU = p.SKU, Price = p.Price,
        DiscountedPrice = p.DiscountedPrice, StockQuantity = p.StockQuantity,
        MainImageUrl = p.MainImageUrl, IsActive = p.IsActive, IsFeatured = p.IsFeatured,
        IsNewProduct = p.IsNewProduct, CategoryId = p.CategoryId, CategoryName = p.Category?.Name,
        BrandId = p.BrandId, BrandName = p.Brand?.Name, TaxRate = p.TaxRate,
        EffectivePrice = p.GetEffectivePrice(), DiscountPercentage = p.GetDiscountPercentage(),
        AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
        ReviewCount = p.Reviews.Count,
        Images = p.Images.OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageDto { Id = i.Id, ProductId = i.ProductId, ImageUrl = i.ImageUrl, DisplayOrder = i.DisplayOrder, IsMain = i.IsMain })
            .ToList()
    };

    public async Task<List<ProductImageDto>> GetProductImagesAsync(int productId)
    {
        var images = await _uow.ProductImages.Query()
            .Where(i => i.ProductId == productId && !i.IsDeleted)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();
        return images.Select(i => new ProductImageDto { Id = i.Id, ProductId = i.ProductId, ImageUrl = i.ImageUrl, DisplayOrder = i.DisplayOrder, IsMain = i.IsMain }).ToList();
    }

    public async Task<ProductImageDto> AddProductImageAsync(int productId, string imageUrl, bool isMain = false)
    {
        var maxOrder = await _uow.ProductImages.Query()
            .Where(i => i.ProductId == productId && !i.IsDeleted)
            .Select(i => (int?)i.DisplayOrder).MaxAsync() ?? -1;

        if (isMain)
        {
            var existing = await _uow.ProductImages.Query()
                .Where(i => i.ProductId == productId && i.IsMain && !i.IsDeleted).ToListAsync();
            foreach (var img in existing) { img.IsMain = false; _uow.ProductImages.Update(img); }
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product != null) { product.MainImageUrl = imageUrl; _uow.Products.Update(product); }
        }

        var image = new ProductImage { ProductId = productId, ImageUrl = imageUrl, IsMain = isMain, DisplayOrder = maxOrder + 1 };
        await _uow.ProductImages.AddAsync(image);
        await _uow.SaveChangesAsync();
        return new ProductImageDto { Id = image.Id, ProductId = image.ProductId, ImageUrl = image.ImageUrl, DisplayOrder = image.DisplayOrder, IsMain = image.IsMain };
    }

    public async Task DeleteProductImageAsync(int imageId)
    {
        var image = await _uow.ProductImages.GetByIdAsync(imageId)
            ?? throw new KeyNotFoundException($"Image {imageId} not found");

        if (image.IsMain)
        {
            var product = await _uow.Products.GetByIdAsync(image.ProductId);
            if (product != null)
            {
                var nextImage = await _uow.ProductImages.Query()
                    .Where(i => i.ProductId == image.ProductId && i.Id != imageId && !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder).FirstOrDefaultAsync();
                if (nextImage != null) { nextImage.IsMain = true; _uow.ProductImages.Update(nextImage); product.MainImageUrl = nextImage.ImageUrl; }
                else product.MainImageUrl = null;
                _uow.Products.Update(product);
            }
        }

        _uow.ProductImages.SoftDelete(image);
        await _uow.SaveChangesAsync();
    }

    public async Task SetMainImageAsync(int productId, int imageId)
    {
        var images = await _uow.ProductImages.Query()
            .Where(i => i.ProductId == productId && !i.IsDeleted).ToListAsync();

        foreach (var img in images) { img.IsMain = img.Id == imageId; _uow.ProductImages.Update(img); }

        var mainImage = images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new KeyNotFoundException($"Image {imageId} not found");

        var product = await _uow.Products.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found");
        product.MainImageUrl = mainImage.ImageUrl;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }

    public async Task ReorderImagesAsync(int productId, List<int> orderedImageIds)
    {
        var images = await _uow.ProductImages.Query()
            .Where(i => i.ProductId == productId && !i.IsDeleted).ToListAsync();

        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            var img = images.FirstOrDefault(x => x.Id == orderedImageIds[i]);
            if (img != null) { img.DisplayOrder = i; _uow.ProductImages.Update(img); }
        }
        await _uow.SaveChangesAsync();
    }

    // ── Variant Management ──────────────────────────────────────

    public async Task<List<ProductVariantDto>> GetProductVariantsAsync(int productId)
    {
        var variants = await _uow.ProductVariants.Query()
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .OrderBy(v => v.Name).ThenBy(v => v.Value)
            .ToListAsync();

        return variants.Select(v => new ProductVariantDto
        {
            Id = v.Id, ProductId = v.ProductId, Name = v.Name, Value = v.Value,
            SKU = v.SKU, PriceAdjustment = v.PriceAdjustment,
            StockQuantity = v.StockQuantity, IsActive = v.IsActive
        }).ToList();
    }

    public async Task<int> AddVariantAsync(int productId, ProductVariantDto dto)
    {
        _ = await _uow.Products.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found");

        var variant = new ProductVariant
        {
            ProductId = productId, Name = dto.Name, Value = dto.Value,
            SKU = dto.SKU, PriceAdjustment = dto.PriceAdjustment,
            StockQuantity = dto.StockQuantity, IsActive = dto.IsActive
        };
        await _uow.ProductVariants.AddAsync(variant);
        await _uow.SaveChangesAsync();
        return variant.Id;
    }

    public async Task UpdateVariantAsync(ProductVariantDto dto)
    {
        var variant = await _uow.ProductVariants.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Variant {dto.Id} not found");

        variant.Name = dto.Name; variant.Value = dto.Value; variant.SKU = dto.SKU;
        variant.PriceAdjustment = dto.PriceAdjustment; variant.StockQuantity = dto.StockQuantity;
        variant.IsActive = dto.IsActive; variant.UpdatedAt = DateTime.UtcNow;

        _uow.ProductVariants.Update(variant);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteVariantAsync(int variantId)
    {
        var variant = await _uow.ProductVariants.GetByIdAsync(variantId)
            ?? throw new KeyNotFoundException($"Variant {variantId} not found");
        _uow.ProductVariants.SoftDelete(variant);
        await _uow.SaveChangesAsync();
    }
}
