namespace Avansas.Domain.Entities;

public class Cart : BaseEntity
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public DateTime? AbandonedEmailSentAt { get; set; }

    public ApplicationUser? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public decimal SubTotal => Items.Where(i => !i.IsDeleted).Sum(i => i.TotalPrice);
    public decimal Total => SubTotal - DiscountAmount;
}
