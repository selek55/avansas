using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IProductQuestionService
{
    Task<List<ProductQuestionDto>> GetApprovedQuestionsAsync(int productId);
    Task<List<ProductQuestionDto>> GetAllQuestionsAsync(bool? isApproved = null);
    Task<int> AskQuestionAsync(string? userId, string askerName, CreateQuestionDto dto);
    Task AnswerQuestionAsync(int questionId, string answerText);
    Task ApproveQuestionAsync(int questionId);
    Task DeleteQuestionAsync(int questionId);
}
