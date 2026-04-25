using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class BannerService : IBannerService
{
    private readonly IUnitOfWork _uow;
    public BannerService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<BannerDto>> GetAllBannersAsync(bool? isActive = null)
    {
        var query = _uow.Banners.Query().Where(b => !b.IsDeleted);
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        var banners = await query.OrderBy(b => b.DisplayOrder).ToListAsync();
        return banners.Select(MapToDto).ToList();
    }

    public async Task<List<BannerDto>> GetActiveBannersAsync()
    {
        var now = DateTime.UtcNow;
        var banners = await _uow.Banners.Query()
            .Where(b => !b.IsDeleted && b.IsActive &&
                (!b.ValidFrom.HasValue || b.ValidFrom <= now) &&
                (!b.ValidTo.HasValue || b.ValidTo >= now))
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();
        return banners.Select(MapToDto).ToList();
    }

    public async Task<BannerDto?> GetBannerByIdAsync(int id)
    {
        var banner = await _uow.Banners.Query()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        return banner == null ? null : MapToDto(banner);
    }

    public async Task<int> CreateBannerAsync(CreateBannerDto dto)
    {
        var banner = new Banner
        {
            Title = dto.Title, SubTitle = dto.SubTitle, ImageUrl = dto.ImageUrl,
            MobileImageUrl = dto.MobileImageUrl, LinkUrl = dto.LinkUrl, ButtonText = dto.ButtonText,
            DisplayOrder = dto.DisplayOrder, IsActive = dto.IsActive,
            ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo
        };
        await _uow.Banners.AddAsync(banner);
        await _uow.SaveChangesAsync();
        return banner.Id;
    }

    public async Task UpdateBannerAsync(UpdateBannerDto dto)
    {
        var banner = await _uow.Banners.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Banner bulunamadı");
        banner.Title = dto.Title; banner.SubTitle = dto.SubTitle; banner.ImageUrl = dto.ImageUrl;
        banner.MobileImageUrl = dto.MobileImageUrl; banner.LinkUrl = dto.LinkUrl;
        banner.ButtonText = dto.ButtonText; banner.DisplayOrder = dto.DisplayOrder;
        banner.IsActive = dto.IsActive; banner.ValidFrom = dto.ValidFrom;
        banner.ValidTo = dto.ValidTo; banner.UpdatedAt = DateTime.UtcNow;
        _uow.Banners.Update(banner);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteBannerAsync(int id)
    {
        var banner = await _uow.Banners.GetByIdAsync(id) ?? throw new KeyNotFoundException("Banner bulunamadı");
        _uow.Banners.SoftDelete(banner);
        await _uow.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(int id)
    {
        var banner = await _uow.Banners.GetByIdAsync(id) ?? throw new KeyNotFoundException("Banner bulunamadı");
        banner.IsActive = !banner.IsActive; banner.UpdatedAt = DateTime.UtcNow;
        _uow.Banners.Update(banner);
        await _uow.SaveChangesAsync();
    }

    private static BannerDto MapToDto(Banner b) => new()
    {
        Id = b.Id, Title = b.Title, SubTitle = b.SubTitle, ImageUrl = b.ImageUrl,
        MobileImageUrl = b.MobileImageUrl, LinkUrl = b.LinkUrl, ButtonText = b.ButtonText,
        DisplayOrder = b.DisplayOrder, IsActive = b.IsActive, ValidFrom = b.ValidFrom, ValidTo = b.ValidTo
    };
}
