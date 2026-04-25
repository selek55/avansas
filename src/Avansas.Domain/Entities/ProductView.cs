namespace Avansas.Domain.Entities;

public class ProductView : BaseEntity
{
    public int ProductId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}
