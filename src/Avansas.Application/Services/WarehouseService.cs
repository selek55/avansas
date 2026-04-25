using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IUnitOfWork _uow;

    public WarehouseService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
    {
        var warehouses = await _uow.Warehouses.Query()
            .Include(w => w.Stocks)
            .OrderBy(w => w.Name)
            .ToListAsync();

        return warehouses.Select(w => new WarehouseDto
        {
            Id = w.Id,
            Name = w.Name,
            Address = w.Address,
            City = w.City,
            Phone = w.Phone,
            IsActive = w.IsActive,
            TotalStock = w.Stocks.Sum(s => s.Quantity)
        }).ToList();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id)
    {
        var w = await _uow.Warehouses.Query()
            .Include(w => w.Stocks)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (w == null) return null;

        return new WarehouseDto
        {
            Id = w.Id,
            Name = w.Name,
            Address = w.Address,
            City = w.City,
            Phone = w.Phone,
            IsActive = w.IsActive,
            TotalStock = w.Stocks.Sum(s => s.Quantity)
        };
    }

    public async Task<int> CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Phone = dto.Phone,
            IsActive = dto.IsActive
        };

        await _uow.Warehouses.AddAsync(warehouse);
        await _uow.SaveChangesAsync();
        return warehouse.Id;
    }

    public async Task UpdateWarehouseAsync(WarehouseDto dto)
    {
        var warehouse = await _uow.Warehouses.GetByIdAsync(dto.Id)
            ?? throw new Exception("Depo bulunamadı");

        warehouse.Name = dto.Name;
        warehouse.Address = dto.Address;
        warehouse.City = dto.City;
        warehouse.Phone = dto.Phone;
        warehouse.IsActive = dto.IsActive;
        warehouse.UpdatedAt = DateTime.UtcNow;

        _uow.Warehouses.Update(warehouse);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteWarehouseAsync(int id)
    {
        var warehouse = await _uow.Warehouses.GetByIdAsync(id)
            ?? throw new Exception("Depo bulunamadı");

        _uow.Warehouses.SoftDelete(warehouse);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<WarehouseStockDto>> GetProductStocksAsync(int productId)
    {
        var stocks = await _uow.WarehouseStocks.Query()
            .Include(s => s.Warehouse)
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .ToListAsync();

        return stocks.Select(MapStockToDto).ToList();
    }

    public async Task<List<WarehouseStockDto>> GetWarehouseStocksAsync(int warehouseId)
    {
        var stocks = await _uow.WarehouseStocks.Query()
            .Include(s => s.Warehouse)
            .Include(s => s.Product)
            .Where(s => s.WarehouseId == warehouseId)
            .OrderBy(s => s.Product.Name)
            .ToListAsync();

        return stocks.Select(MapStockToDto).ToList();
    }

    public async Task UpdateStockAsync(int warehouseId, int productId, int quantity)
    {
        var stock = await _uow.WarehouseStocks.Query()
            .FirstOrDefaultAsync(s => s.WarehouseId == warehouseId && s.ProductId == productId);

        if (stock == null)
        {
            stock = new WarehouseStock
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = quantity
            };
            await _uow.WarehouseStocks.AddAsync(stock);
        }
        else
        {
            stock.Quantity = quantity;
            stock.UpdatedAt = DateTime.UtcNow;
            _uow.WarehouseStocks.Update(stock);
        }

        await _uow.SaveChangesAsync();
    }

    private static WarehouseStockDto MapStockToDto(WarehouseStock s) => new()
    {
        Id = s.Id,
        WarehouseId = s.WarehouseId,
        WarehouseName = s.Warehouse.Name,
        ProductId = s.ProductId,
        ProductName = s.Product.Name,
        Quantity = s.Quantity
    };
}
