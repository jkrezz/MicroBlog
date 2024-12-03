using Blog.Models;

namespace Blog.Services.Interfaces;
public interface IPostService
{
    Task<PostModel?> GetPostByIdAsync(Guid id);
    Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId);
    Task<List<PostModel>> GetPublishedPostsAsync();
    Task<PostModel> CreatePostAsync(string authorId, CreatePostRequest postRequest);
    Task<PostModel?> UpdatePostAsync(Guid postId, string authorId, UpdatePost updatePost);
    Task<bool> DeleteImageAsync(Guid postId, Guid imageId, string authorId);
    Task<List<ImageModel>> AddImagesToPostAsync(Guid postId, string authorId, List<IFormFile> images);
    Task<PostModel?> PublishPostAsync(Guid postId, string authorId, PublishPostRequest request);
}
