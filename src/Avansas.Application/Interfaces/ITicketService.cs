using Avansas.Application.DTOs;
using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public interface ITicketService
{
    Task<List<TicketDto>> GetUserTicketsAsync(string userId);
    Task<PagedResult<TicketDto>> GetAllTicketsAsync(int page = 1, int pageSize = 20, TicketStatus? status = null);
    Task<TicketDto?> GetTicketByIdAsync(int id);
    Task<int> CreateTicketAsync(string userId, CreateTicketDto dto);
    Task AddMessageAsync(int ticketId, string senderId, string message, bool isAdmin);
    Task UpdateStatusAsync(int ticketId, TicketStatus status);
}
