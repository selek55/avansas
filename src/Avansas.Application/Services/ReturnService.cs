using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ReturnService : IReturnService
{
    private readonly IUnitOfWork _uow;
    public ReturnService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ReturnRequestDto>> GetAllReturnsAsync(ReturnStatus? status = null)
    {
        var query = _uow.ReturnRequests.Query()
            .Include(r => r.Items)
            .Include(r => r.Order)
            .Include(r => r.User)
            .Where(r => !r.IsDeleted);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var returns = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return returns.Select(MapToDto).ToList();
    }

    public async Task<List<ReturnRequestDto>> GetUserReturnsAsync(string userId)
    {
        var returns = await _uow.ReturnRequests.Query()
            .Include(r => r.Items)
            .Include(r => r.Order)
            .Include(r => r.User)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return returns.Select(MapToDto).ToList();
    }

    public async Task<ReturnRequestDto?> GetReturnByIdAsync(int id)
    {
        var ret = await _uow.ReturnRequests.Query()
            .Include(r => r.Items).ThenInclude(i => i.OrderItem).ThenInclude(oi => oi.Product)
            .Include(r => r.Order)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        return ret == null ? null : MapToDto(ret);
    }

    public async Task<int> CreateReturnAsync(string userId, CreateReturnRequestDto dto)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId && !o.IsDeleted);

        if (order == null)
            throw new InvalidOperationException("Sipariş bulunamadı.");

        var returnRequest = new ReturnRequest
        {
            OrderId = dto.OrderId,
            UserId = userId,
            Reason = dto.Reason,
            Status = ReturnStatus.Pending
        };

        decimal refundAmount = 0;
        var returnItems = new List<ReturnItem>();

        foreach (var item in dto.Items)
        {
            var orderItem = order.Items.FirstOrDefault(oi => oi.Id == item.OrderItemId);
            if (orderItem == null) continue;

            refundAmount += orderItem.UnitPrice * item.Quantity;
            returnItems.Add(new ReturnItem
            {
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity,
                Reason = item.Reason
            });
        }

        returnRequest.RefundAmount = refundAmount;
        await _uow.ReturnRequests.AddAsync(returnRequest);
        await _uow.SaveChangesAsync();

        foreach (var ri in returnItems)
        {
            ri.ReturnRequestId = returnRequest.Id;
            await _uow.ReturnItems.AddAsync(ri);
        }
        await _uow.SaveChangesAsync();

        return returnRequest.Id;
    }

    public async Task ApproveReturnAsync(int id, string? adminNotes)
    {
        var ret = await _uow.ReturnRequests.GetByIdAsync(id);
        if (ret == null || ret.IsDeleted) throw new InvalidOperationException("İade talebi bulunamadı.");

        ret.Status = ReturnStatus.Approved;
        ret.AdminNotes = adminNotes;
        ret.ProcessedAt = DateTime.UtcNow;
        _uow.ReturnRequests.Update(ret);
        await _uow.SaveChangesAsync();
    }

    public async Task RejectReturnAsync(int id, string? adminNotes)
    {
        var ret = await _uow.ReturnRequests.GetByIdAsync(id);
        if (ret == null || ret.IsDeleted) throw new InvalidOperationException("İade talebi bulunamadı.");

        ret.Status = ReturnStatus.Rejected;
        ret.AdminNotes = adminNotes;
        ret.ProcessedAt = DateTime.UtcNow;
        _uow.ReturnRequests.Update(ret);
        await _uow.SaveChangesAsync();
    }

    public async Task RefundReturnAsync(int id)
    {
        var ret = await _uow.ReturnRequests.GetByIdAsync(id);
        if (ret == null || ret.IsDeleted) throw new InvalidOperationException("İade talebi bulunamadı.");

        ret.Status = ReturnStatus.Refunded;
        ret.ProcessedAt = DateTime.UtcNow;
        _uow.ReturnRequests.Update(ret);
        await _uow.SaveChangesAsync();
    }

    public async Task CancelReturnAsync(int id)
    {
        var ret = await _uow.ReturnRequests.GetByIdAsync(id);
        if (ret == null || ret.IsDeleted) throw new InvalidOperationException("İade talebi bulunamadı.");

        ret.Status = ReturnStatus.Cancelled;
        ret.ProcessedAt = DateTime.UtcNow;
        _uow.ReturnRequests.Update(ret);
        await _uow.SaveChangesAsync();
    }

    private static ReturnRequestDto MapToDto(ReturnRequest r) => new()
    {
        Id = r.Id,
        OrderId = r.OrderId,
        OrderNumber = r.Order?.OrderNumber ?? string.Empty,
        UserId = r.UserId,
        UserFullName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
        Reason = r.Reason,
        AdminNotes = r.AdminNotes,
        Status = r.Status,
        RefundAmount = r.RefundAmount,
        ProcessedAt = r.ProcessedAt,
        CreatedAt = r.CreatedAt,
        Items = r.Items.Select(i => new ReturnItemDto
        {
            Id = i.Id,
            OrderItemId = i.OrderItemId,
            ProductName = i.OrderItem?.ProductName ?? i.OrderItem?.Product?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.OrderItem?.UnitPrice ?? 0,
            Reason = i.Reason
        }).ToList()
    };
}
