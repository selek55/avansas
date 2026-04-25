using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IBlogService
{
    Task<PagedResult<BlogPostDto>> GetPublishedPostsAsync(int page = 1, int pageSize = 10, int? categoryId = null);
    Task<BlogPostDto?> GetPostBySlugAsync(string slug);
    Task<BlogPostDto?> GetPostByIdAsync(int id);
    Task<List<BlogPostDto>> GetAllPostsAsync();
    Task<int> CreatePostAsync(string authorId, CreateBlogPostDto dto);
    Task UpdatePostAsync(UpdateBlogPostDto dto);
    Task DeletePostAsync(int id);
    Task<List<BlogCategoryDto>> GetCategoriesAsync();
    Task<int> CreateCategoryAsync(CreateBlogCategoryDto dto);
    Task DeleteCategoryAsync(int id);
}
