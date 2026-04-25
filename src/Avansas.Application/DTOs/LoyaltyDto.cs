using Avansas.Domain.Enums;

namespace Avansas.Application.DTOs;

public class LoyaltyTransactionDto
{
    public int Id { get; set; }
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public LoyaltyType Type { get; set; }
    public string TypeText { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoyaltySummaryDto
{
    public int TotalPoints { get; set; }
    public int EarnedPoints { get; set; }
    public int SpentPoints { get; set; }
    public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = new();
}
