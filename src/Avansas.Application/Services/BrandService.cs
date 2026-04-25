using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _uow;
    public BrandService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<BrandDto>> GetAllBrandsAsync(bool? isActive = null)
    {
        var query = _uow.Brands.Query().Where(b => !b.IsDeleted);
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        var brands = await query.Include(b => b.Products).OrderBy(b => b.Name).ToListAsync();
        return brands.Select(MapToDto).ToList();
    }

    public async Task<BrandDto?> GetBrandByIdAsync(int id)
    {
        var brand = await _uow.Brands.Query()
            .Include(b => b.Products).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        return brand == null ? null : MapToDto(brand);
    }

    public async Task<int> CreateBrandAsync(CreateBrandDto dto)
    {
        var brand = new Brand
        {
            Name = dto.Name,
            Slug = string.IsNullOrEmpty(dto.Slug) ? ToSlug(dto.Name) : dto.Slug,
            LogoUrl = dto.LogoUrl, Description = dto.Description, IsActive = dto.IsActive
        };
        await _uow.Brands.AddAsync(brand);
        await _uow.SaveChangesAsync();
        return brand.Id;
    }

    public async Task UpdateBrandAsync(UpdateBrandDto dto)
    {
        var brand = await _uow.Brands.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Marka bulunamadı");
        brand.Name = dto.Name;
        brand.Slug = string.IsNullOrEmpty(dto.Slug) ? ToSlug(dto.Name) : dto.Slug;
        brand.LogoUrl = dto.LogoUrl; brand.Description = dto.Description;
        brand.IsActive = dto.IsActive; brand.UpdatedAt = DateTime.UtcNow;
        _uow.Brands.Update(brand);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteBrandAsync(int id)
    {
        var brand = await _uow.Brands.GetByIdAsync(id) ?? throw new KeyNotFoundException("Marka bulunamadı");
        _uow.Brands.SoftDelete(brand);
        await _uow.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(int id)
    {
        var brand = await _uow.Brands.GetByIdAsync(id) ?? throw new KeyNotFoundException("Marka bulunamadı");
        brand.IsActive = !brand.IsActive; brand.UpdatedAt = DateTime.UtcNow;
        _uow.Brands.Update(brand);
        await _uow.SaveChangesAsync();
    }

    private static BrandDto MapToDto(Brand b) => new()
    {
        Id = b.Id, Name = b.Name, Slug = b.Slug, LogoUrl = b.LogoUrl,
        Description = b.Description, IsActive = b.IsActive,
        ProductCount = b.Products.Count(p => !p.IsDeleted)
    };

    private static string ToSlug(string name) =>
        name.ToLower()
            .Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ö", "o").Replace("ç", "c").Replace("ı", "i")
            .Replace(" ", "-")
            .Replace("Ş", "s").Replace("Ğ", "g").Replace("Ü", "u")
            .Replace("Ö", "o").Replace("Ç", "c").Replace("İ", "i");
}
