using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _uow;

    public UserService(UserManager<ApplicationUser> userManager, IUnitOfWork uow)
    {
        _userManager = userManager;
        _uow = uow;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var query = _userManager.Users.Where(u => u.IsActive);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var orders = await _uow.Orders.Query().Where(o => o.UserId == user.Id).ToListAsync();
            dtos.Add(new UserDto
            {
                Id = user.Id, FirstName = user.FirstName, LastName = user.LastName,
                FullName = user.FullName, Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber, CompanyName = user.CompanyName,
                IsCorporate = user.IsCorporate, IsActive = user.IsActive,
                CreatedAt = user.CreatedAt, OrderCount = orders.Count,
                TotalSpent = orders.Sum(o => o.Total), Roles = roles.ToList()
            });
        }

        return new PagedResult<UserDto> { Items = dtos, TotalCount = total, PageNumber = page, PageSize = pageSize };
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var orders = await _uow.Orders.Query().Where(o => o.UserId == id).ToListAsync();

        return new UserDto
        {
            Id = user.Id, FirstName = user.FirstName, LastName = user.LastName,
            FullName = user.FullName, Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber, CompanyName = user.CompanyName,
            IsCorporate = user.IsCorporate, IsActive = user.IsActive,
            CreatedAt = user.CreatedAt, OrderCount = orders.Count,
            TotalSpent = orders.Sum(o => o.Total), Roles = roles.ToList()
        };
    }

    public async Task<bool> SetUserActiveStatusAsync(string userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<List<AddressDto>> GetUserAddressesAsync(string userId)
    {
        var addresses = await _uow.Addresses.Query()
            .Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ToListAsync();
        return addresses.Select(a => new AddressDto
        {
            Id = a.Id, Title = a.Title, FirstName = a.FirstName, LastName = a.LastName,
            CompanyName = a.CompanyName, Phone = a.Phone, AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2, City = a.City, District = a.District,
            PostalCode = a.PostalCode, Country = a.Country, IsDefault = a.IsDefault,
            IsBillingAddress = a.IsBillingAddress, TaxNumber = a.TaxNumber, TaxOffice = a.TaxOffice
        }).ToList();
    }

    public async Task<int> AddAddressAsync(string userId, AddressDto dto)
    {
        if (dto.IsDefault)
        {
            var existing = await _uow.Addresses.Query().Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
            foreach (var addr in existing) { addr.IsDefault = false; _uow.Addresses.Update(addr); }
        }
        var address = new Address
        {
            UserId = userId, Title = dto.Title, FirstName = dto.FirstName, LastName = dto.LastName,
            CompanyName = dto.CompanyName, Phone = dto.Phone, AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2, City = dto.City, District = dto.District,
            PostalCode = dto.PostalCode, Country = dto.Country, IsDefault = dto.IsDefault,
            IsBillingAddress = dto.IsBillingAddress, TaxNumber = dto.TaxNumber, TaxOffice = dto.TaxOffice
        };
        await _uow.Addresses.AddAsync(address);
        await _uow.SaveChangesAsync();
        return address.Id;
    }

    public async Task UpdateAddressAsync(string userId, AddressDto dto)
    {
        var address = await _uow.Addresses.Query().FirstOrDefaultAsync(a => a.Id == dto.Id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Adres bulunamadı");
        address.Title = dto.Title; address.FirstName = dto.FirstName; address.LastName = dto.LastName;
        address.CompanyName = dto.CompanyName; address.Phone = dto.Phone;
        address.AddressLine1 = dto.AddressLine1; address.AddressLine2 = dto.AddressLine2;
        address.City = dto.City; address.District = dto.District; address.PostalCode = dto.PostalCode;
        address.IsDefault = dto.IsDefault; address.IsBillingAddress = dto.IsBillingAddress;
        address.TaxNumber = dto.TaxNumber; address.TaxOffice = dto.TaxOffice;
        _uow.Addresses.Update(address);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteAddressAsync(string userId, int addressId)
    {
        var address = await _uow.Addresses.Query().FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
            ?? throw new KeyNotFoundException("Adres bulunamadı");
        _uow.Addresses.SoftDelete(address);
        await _uow.SaveChangesAsync();
    }

    public async Task SetDefaultAddressAsync(string userId, int addressId)
    {
        var addresses = await _uow.Addresses.Query().Where(a => a.UserId == userId).ToListAsync();
        foreach (var addr in addresses)
        {
            addr.IsDefault = addr.Id == addressId;
            _uow.Addresses.Update(addr);
        }
        await _uow.SaveChangesAsync();
    }
}
