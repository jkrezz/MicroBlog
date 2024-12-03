using Blog.Models;
using Blog.Repositories.Interfaces;
namespace Blog.Repositories;

public class PostRepository : IPostRepository
{
    private static readonly List<PostModel> _posts = new();

    public async Task<PostModel?> GetPostByIdAsync(Guid postId)
    {
        return _posts.FirstOrDefault(p => p.PostId == postId);
    }

    public async Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId)
    {
        return _posts.Where(p => p.AuthorId == authorId).ToList();
    }

    public async Task<List<PostModel>> GetPublishedPostsAsync()
    {
        return _posts.Where(p => p.Status == "Published").ToList();
    }

    public async Task AddPostAsync(PostModel post)
    {
        _posts.Add(post);
    }

    public async Task UpdatePostAsync(PostModel post)
    {
        var existingPost = _posts.FirstOrDefault(p => p.PostId == post.PostId);
        if (existingPost != null)
        {
            _posts.Remove(existingPost);
            _posts.Add(post);
        }
    }

    public async Task DeletePostAsync(Guid postId)
    {
        var post = _posts.FirstOrDefault(p => p.PostId == postId);
        if (post != null)
        {
            _posts.Remove(post);
        }
    }
}
