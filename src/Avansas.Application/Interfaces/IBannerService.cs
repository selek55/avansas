using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IBannerService
{
    Task<List<BannerDto>> GetAllBannersAsync(bool? isActive = null);
    Task<List<BannerDto>> GetActiveBannersAsync();
    Task<BannerDto?> GetBannerByIdAsync(int id);
    Task<int> CreateBannerAsync(CreateBannerDto dto);
    Task UpdateBannerAsync(UpdateBannerDto dto);
    Task DeleteBannerAsync(int id);
    Task ToggleActiveAsync(int id);
}
