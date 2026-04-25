using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ReviewDto>> GetApprovedReviewsAsync(int productId)
    {
        var reviews = await _uow.Reviews.Query()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return reviews.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<ReviewDto>> GetAllReviewsAsync(int page = 1, int pageSize = 20, bool? isApproved = null)
    {
        var query = _uow.Reviews.Query()
            .Include(r => r.User).Include(r => r.Product)
            .Where(r => !r.IsDeleted);

        if (isApproved.HasValue) query = query.Where(r => r.IsApproved == isApproved.Value);
        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<ReviewDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, PageNumber = page, PageSize = pageSize
        };
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var ratings = await _uow.Reviews.Query()
            .Where(r => r.ProductId == productId && r.IsApproved && !r.IsDeleted)
            .Select(r => r.Rating).ToListAsync();
        return ratings.Count > 0 ? ratings.Average() : 0;
    }

    public async Task<int> GetReviewCountAsync(int productId) =>
        await _uow.Reviews.Query()
            .CountAsync(r => r.ProductId == productId && r.IsApproved && !r.IsDeleted);

    public async Task CreateReviewAsync(CreateReviewDto dto)
    {
        if (await HasUserReviewedAsync(dto.ProductId, dto.UserId))
            throw new InvalidOperationException("Bu ürün için zaten yorum yaptınız");

        var review = new Review
        {
            ProductId = dto.ProductId, UserId = dto.UserId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Title = dto.Title, Comment = dto.Comment,
            IsApproved = false
        };
        await _uow.Reviews.AddAsync(review);
        await _uow.SaveChangesAsync();
    }

    public async Task ApproveReviewAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Yorum bulunamadı");
        review.IsApproved = true;
        review.UpdatedAt = DateTime.UtcNow;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteReviewAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Yorum bulunamadı");
        _uow.Reviews.SoftDelete(review);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> HasUserReviewedAsync(int productId, string userId) =>
        await _uow.Reviews.Query()
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId && !r.IsDeleted);

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id, ProductId = r.ProductId,
        ProductName = r.Product?.Name ?? string.Empty,
        UserId = r.UserId,
        UserFullName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Kullanıcı",
        Rating = r.Rating, Title = r.Title, Comment = r.Comment,
        IsApproved = r.IsApproved, CreatedAt = r.CreatedAt
    };
}
