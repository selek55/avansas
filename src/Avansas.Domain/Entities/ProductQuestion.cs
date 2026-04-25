namespace Avansas.Domain.Entities;

public class ProductQuestion : BaseEntity
{
    public int ProductId { get; set; }
    public string? UserId { get; set; }
    public string AskerName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public bool IsApproved { get; set; }
    public Product Product { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
