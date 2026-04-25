using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public LoyaltyType Type { get; set; }
    public int? OrderId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Order? Order { get; set; }
}
