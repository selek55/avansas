using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface ICouponService
{
    Task<List<CouponDto>> GetAllCouponsAsync();
    Task<CouponDto?> GetCouponByIdAsync(int id);
    Task<int> CreateCouponAsync(CreateCouponDto dto);
    Task UpdateCouponAsync(UpdateCouponDto dto);
    Task DeleteCouponAsync(int id);
    Task ToggleActiveAsync(int id);
}
