using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _uow;
    public CouponService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<CouponDto>> GetAllCouponsAsync()
    {
        var coupons = await _uow.Coupons.Query()
            .Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt).ToListAsync();
        return coupons.Select(MapToDto).ToList();
    }

    public async Task<CouponDto?> GetCouponByIdAsync(int id)
    {
        var coupon = await _uow.Coupons.Query()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return coupon == null ? null : MapToDto(coupon);
    }

    public async Task<int> CreateCouponAsync(CreateCouponDto dto)
    {
        var coupon = new Coupon
        {
            Code = dto.Code.ToUpper().Trim(), Description = dto.Description,
            DiscountType = dto.DiscountType, DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount, MaxDiscountAmount = dto.MaxDiscountAmount,
            UsageLimit = dto.UsageLimit, ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo,
            IsActive = dto.IsActive
        };
        await _uow.Coupons.AddAsync(coupon);
        await _uow.SaveChangesAsync();
        return coupon.Id;
    }

    public async Task UpdateCouponAsync(UpdateCouponDto dto)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Kupon bulunamadı");
        coupon.Code = dto.Code.ToUpper().Trim(); coupon.Description = dto.Description;
        coupon.DiscountType = dto.DiscountType; coupon.DiscountValue = dto.DiscountValue;
        coupon.MinOrderAmount = dto.MinOrderAmount; coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
        coupon.UsageLimit = dto.UsageLimit; coupon.ValidFrom = dto.ValidFrom;
        coupon.ValidTo = dto.ValidTo; coupon.IsActive = dto.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;
        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteCouponAsync(int id)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id) ?? throw new KeyNotFoundException("Kupon bulunamadı");
        _uow.Coupons.SoftDelete(coupon);
        await _uow.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(int id)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id) ?? throw new KeyNotFoundException("Kupon bulunamadı");
        coupon.IsActive = !coupon.IsActive; coupon.UpdatedAt = DateTime.UtcNow;
        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync();
    }

    private static CouponDto MapToDto(Coupon c) => new()
    {
        Id = c.Id, Code = c.Code, Description = c.Description, DiscountType = c.DiscountType,
        DiscountValue = c.DiscountValue, MinOrderAmount = c.MinOrderAmount,
        MaxDiscountAmount = c.MaxDiscountAmount, UsageLimit = c.UsageLimit,
        UsedCount = c.UsedCount, ValidFrom = c.ValidFrom, ValidTo = c.ValidTo, IsActive = c.IsActive
    };
}
