namespace Avansas.Domain.Entities;

public class Review : BaseEntity
{
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = false;

    public Product Product { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
