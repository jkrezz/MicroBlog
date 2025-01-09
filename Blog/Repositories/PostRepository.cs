using Blog.Data;
using Blog.Models;
using Blog.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly PostsDbContext _context;

        public PostRepository(PostsDbContext context)
        {
            _context = context;
        }

        public async Task<PostModel?> GetPostByIdAsync(Guid postId)
        {
            return await _context.Posts.FindAsync(postId);
        }

        public async Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId)
        {
            return await _context.Posts.Where(p => p.AuthorId == authorId).ToListAsync();
        }

        public async Task<List<PostModel>> GetPublishedPostsAsync()
        {
            return await _context.Posts.Where(p => p.Status == "Published").ToListAsync();
        }

        public async Task AddPostAsync(PostModel post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePostAsync(PostModel post)
        {
            var existingPost = await _context.Posts.FindAsync(post.PostId);
            if (existingPost != null)
            {
                _context.Entry(existingPost).CurrentValues.SetValues(post);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task AddImageAsync(ImageModel image)
        {
            _context.Set<ImageModel>().Add(image);
            await _context.SaveChangesAsync();
        }
        
        public async Task DeletePostAsync(Guid postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<PostModel?> GetPostByIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.Posts
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
        }
    }
}