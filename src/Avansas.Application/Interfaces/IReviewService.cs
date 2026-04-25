using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IReviewService
{
    Task<List<ReviewDto>> GetApprovedReviewsAsync(int productId);
    Task<PagedResult<ReviewDto>> GetAllReviewsAsync(int page = 1, int pageSize = 20, bool? isApproved = null);
    Task<double> GetAverageRatingAsync(int productId);
    Task<int> GetReviewCountAsync(int productId);
    Task CreateReviewAsync(CreateReviewDto dto);
    Task ApproveReviewAsync(int reviewId);
    Task DeleteReviewAsync(int reviewId);
    Task<bool> HasUserReviewedAsync(int productId, string userId);
}
