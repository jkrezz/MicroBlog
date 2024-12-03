using Blog.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Repositories.Interfaces
{
    public interface IPostRepository
    {
        Task<PostModel?> GetPostByIdAsync(Guid postId);
        Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId);
        Task<List<PostModel>> GetPublishedPostsAsync();
        Task AddPostAsync(PostModel post);
        Task UpdatePostAsync(PostModel post);
        Task DeletePostAsync(Guid postId);
    }
}