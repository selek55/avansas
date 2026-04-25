using Avansas.Application.DTOs;
using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public interface IReturnService
{
    Task<List<ReturnRequestDto>> GetAllReturnsAsync(ReturnStatus? status = null);
    Task<List<ReturnRequestDto>> GetUserReturnsAsync(string userId);
    Task<ReturnRequestDto?> GetReturnByIdAsync(int id);
    Task<int> CreateReturnAsync(string userId, CreateReturnRequestDto dto);
    Task ApproveReturnAsync(int id, string? adminNotes);
    Task RejectReturnAsync(int id, string? adminNotes);
    Task RefundReturnAsync(int id);
    Task CancelReturnAsync(int id);
}
