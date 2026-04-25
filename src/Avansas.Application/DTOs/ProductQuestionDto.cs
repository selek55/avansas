namespace Avansas.Application.DTOs;

public class ProductQuestionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AskerName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateQuestionDto
{
    public int ProductId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
}
