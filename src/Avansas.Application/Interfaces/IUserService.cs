using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<bool> SetUserActiveStatusAsync(string userId, bool isActive);
    Task<List<AddressDto>> GetUserAddressesAsync(string userId);
    Task<int> AddAddressAsync(string userId, AddressDto dto);
    Task UpdateAddressAsync(string userId, AddressDto dto);
    Task DeleteAddressAsync(string userId, int addressId);
    Task SetDefaultAddressAsync(string userId, int addressId);
}
