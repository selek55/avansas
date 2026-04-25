namespace Avansas.Application.DTOs;

public class StockNotificationDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsNotified { get; set; }
    public DateTime CreatedAt { get; set; }
}
