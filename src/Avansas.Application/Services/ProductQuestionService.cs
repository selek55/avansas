using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ProductQuestionService : IProductQuestionService
{
    private readonly IUnitOfWork _uow;
    public ProductQuestionService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ProductQuestionDto>> GetApprovedQuestionsAsync(int productId)
    {
        var questions = await _uow.ProductQuestions.Query()
            .Include(q => q.Product)
            .Where(q => q.ProductId == productId && q.IsApproved && !q.IsDeleted)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return questions.Select(MapToDto).ToList();
    }

    public async Task<List<ProductQuestionDto>> GetAllQuestionsAsync(bool? isApproved = null)
    {
        var query = _uow.ProductQuestions.Query()
            .Include(q => q.Product)
            .Where(q => !q.IsDeleted);

        if (isApproved.HasValue) query = query.Where(q => q.IsApproved == isApproved.Value);
        query = query.OrderByDescending(q => q.CreatedAt);

        var questions = await query.ToListAsync();
        return questions.Select(MapToDto).ToList();
    }

    public async Task<int> AskQuestionAsync(string? userId, string askerName, CreateQuestionDto dto)
    {
        var question = new ProductQuestion
        {
            ProductId = dto.ProductId, UserId = userId,
            AskerName = askerName, QuestionText = dto.QuestionText,
            IsApproved = false
        };
        await _uow.ProductQuestions.AddAsync(question);
        await _uow.SaveChangesAsync();
        return question.Id;
    }

    public async Task AnswerQuestionAsync(int questionId, string answerText)
    {
        var question = await _uow.ProductQuestions.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Soru bulunamadı");
        question.AnswerText = answerText;
        question.AnsweredAt = DateTime.UtcNow;
        question.UpdatedAt = DateTime.UtcNow;
        _uow.ProductQuestions.Update(question);
        await _uow.SaveChangesAsync();
    }

    public async Task ApproveQuestionAsync(int questionId)
    {
        var question = await _uow.ProductQuestions.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Soru bulunamadı");
        question.IsApproved = true;
        question.UpdatedAt = DateTime.UtcNow;
        _uow.ProductQuestions.Update(question);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int questionId)
    {
        var question = await _uow.ProductQuestions.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Soru bulunamadı");
        _uow.ProductQuestions.SoftDelete(question);
        await _uow.SaveChangesAsync();
    }

    private static ProductQuestionDto MapToDto(ProductQuestion q) => new()
    {
        Id = q.Id, ProductId = q.ProductId,
        ProductName = q.Product?.Name ?? string.Empty,
        AskerName = q.AskerName, QuestionText = q.QuestionText,
        AnswerText = q.AnswerText, AnsweredAt = q.AnsweredAt,
        IsApproved = q.IsApproved, CreatedAt = q.CreatedAt
    };
}
