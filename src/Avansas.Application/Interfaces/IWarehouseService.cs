using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllWarehousesAsync();
    Task<WarehouseDto?> GetWarehouseByIdAsync(int id);
    Task<int> CreateWarehouseAsync(CreateWarehouseDto dto);
    Task UpdateWarehouseAsync(WarehouseDto dto);
    Task DeleteWarehouseAsync(int id);
    Task<List<WarehouseStockDto>> GetProductStocksAsync(int productId);
    Task<List<WarehouseStockDto>> GetWarehouseStocksAsync(int warehouseId);
    Task UpdateStockAsync(int warehouseId, int productId, int quantity);
}
