using Avansas.Domain.Enums;

namespace Avansas.Application.DTOs;

public class ReturnRequestDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public ReturnStatus Status { get; set; }
    public string StatusText => Status switch
    {
        ReturnStatus.Pending => "Beklemede",
        ReturnStatus.Approved => "Onaylandı",
        ReturnStatus.Rejected => "Reddedildi",
        ReturnStatus.Refunded => "İade Edildi",
        ReturnStatus.Cancelled => "İptal",
        _ => "Bilinmiyor"
    };
    public decimal RefundAmount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReturnItemDto> Items { get; set; } = new();
}

public class ReturnItemDto
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Reason { get; set; }
}

public class CreateReturnRequestDto
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<CreateReturnItemDto> Items { get; set; } = new();
}

public class CreateReturnItemDto
{
    public int OrderItemId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
