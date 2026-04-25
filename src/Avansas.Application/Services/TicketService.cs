using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _uow;
    public TicketService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<TicketDto>> GetUserTicketsAsync(string userId)
    {
        var tickets = await _uow.SupportTickets.Query()
            .Include(t => t.Messages)
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return tickets.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<TicketDto>> GetAllTicketsAsync(int page = 1, int pageSize = 20, TicketStatus? status = null)
    {
        var query = _uow.SupportTickets.Query()
            .Include(t => t.User).Include(t => t.Messages)
            .Where(t => !t.IsDeleted);

        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        query = query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<TicketDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, PageNumber = page, PageSize = pageSize
        };
    }

    public async Task<TicketDto?> GetTicketByIdAsync(int id)
    {
        var ticket = await _uow.SupportTickets.Query()
            .Include(t => t.User).Include(t => t.Order)
            .Include(t => t.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        return ticket == null ? null : MapToDto(ticket);
    }

    public async Task<int> CreateTicketAsync(string userId, CreateTicketDto dto)
    {
        var ticket = new SupportTicket
        {
            UserId = userId, Subject = dto.Subject,
            Status = TicketStatus.Open, Priority = TicketPriority.Normal,
            OrderId = dto.OrderId
        };
        await _uow.SupportTickets.AddAsync(ticket);
        await _uow.SaveChangesAsync();

        var message = new TicketMessage
        {
            TicketId = ticket.Id, SenderId = userId,
            Message = dto.Message, IsAdminReply = false
        };
        await _uow.TicketMessages.AddAsync(message);
        await _uow.SaveChangesAsync();

        return ticket.Id;
    }

    public async Task AddMessageAsync(int ticketId, string senderId, string message, bool isAdmin)
    {
        var ticket = await _uow.SupportTickets.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException("Destek talebi bulunamadı");

        var msg = new TicketMessage
        {
            TicketId = ticketId, SenderId = senderId,
            Message = message, IsAdminReply = isAdmin
        };
        await _uow.TicketMessages.AddAsync(msg);

        ticket.UpdatedAt = DateTime.UtcNow;
        _uow.SupportTickets.Update(ticket);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int ticketId, TicketStatus status)
    {
        var ticket = await _uow.SupportTickets.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException("Destek talebi bulunamadı");
        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        _uow.SupportTickets.Update(ticket);
        await _uow.SaveChangesAsync();
    }

    private static string GetStatusText(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Açık",
        TicketStatus.InProgress => "İşlemde",
        TicketStatus.WaitingCustomer => "Müşteri Bekleniyor",
        TicketStatus.Resolved => "Çözüldü",
        TicketStatus.Closed => "Kapatıldı",
        _ => status.ToString()
    };

    private static string GetPriorityText(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Düşük",
        TicketPriority.Normal => "Normal",
        TicketPriority.High => "Yüksek",
        TicketPriority.Urgent => "Acil",
        _ => priority.ToString()
    };

    private static TicketDto MapToDto(SupportTicket t) => new()
    {
        Id = t.Id, UserId = t.UserId,
        UserFullName = t.User != null ? t.User.FullName : string.Empty,
        UserEmail = t.User?.Email ?? string.Empty,
        Subject = t.Subject, Status = t.Status, StatusText = GetStatusText(t.Status),
        Priority = t.Priority, PriorityText = GetPriorityText(t.Priority),
        OrderId = t.OrderId, OrderNumber = t.Order?.OrderNumber,
        CreatedAt = t.CreatedAt,
        LastMessageAt = t.Messages.Any() ? t.Messages.Max(m => m.CreatedAt) : null,
        MessageCount = t.Messages.Count,
        Messages = t.Messages.OrderBy(m => m.CreatedAt).Select(m => new TicketMessageDto
        {
            Id = m.Id,
            SenderName = m.Sender != null ? m.Sender.FullName : string.Empty,
            Message = m.Message, IsAdminReply = m.IsAdminReply,
            CreatedAt = m.CreatedAt
        }).ToList()
    };
}
