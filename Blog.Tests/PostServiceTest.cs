using Blog.Data;
using Blog.Exceptions;
using Blog.Models;
using Blog.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Blog.Repositories;
using Blog.Repositories.Interfaces;
using Moq;

namespace Blog.Tests
{
    public class TestBlogDbContext : PostsDbContext
    {
        public TestBlogDbContext(DbContextOptions<PostsDbContext> options)
            : base(options, null)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }

    [TestFixture]
    public class PostServiceTests
    {
        private DbContextOptions<PostsDbContext> _options;
        private PostsDbContext _context;
        private PostService _postService;

        [SetUp]
        public void SetUp()
        {
            _options = new DbContextOptionsBuilder<PostsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new TestBlogDbContext(_options);
            _context.Database.EnsureCreated();
            IPostRepository postRepository = new PostRepository(_context);
            _postService = new PostService(null, postRepository);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task CreatePostAsync_WithValidData_ShouldSaveToDatabase()
        {
            // Arrange
            var idempotencyKey = "key";
            var request = new CreatePostRequest
            {
                IdempotencyKey = idempotencyKey,
                Title = "Test",
                Content = "Content"
            };
            var authorId = Guid.NewGuid().ToString();

            // Act
            var result = await _postService.CreatePostAsync(authorId, request);

            // Assert
            var savedPost = _context.Posts.FirstOrDefault(p => p.Title == "Test");
            Assert.That(savedPost, Is.Not.Null);
            Assert.That(savedPost.Title, Is.EqualTo(request.Title));
            Assert.That(savedPost.Content, Is.EqualTo(request.Content));
            Assert.That(savedPost.AuthorId.ToString(), Is.EqualTo(authorId));
        }

        [Test]
        public void CreatePostAsync_WithDuplicateIdempotencyKey_ShouldThrowConflictException()
        {
            // Arrange
            var idempotencyKey = "key";
            var post = new PostModel
            {
                PostId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Title = "Existing Post",
                Content = "Content",
                IdempotencyKey = idempotencyKey,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            _context.SaveChanges();

            var request = new CreatePostRequest
            {
                IdempotencyKey = idempotencyKey,
                Title = "New Post",
                Content = "New Content"
            };

            var authorId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.ThrowsAsync<ConflictException>(async () =>
                await _postService.CreatePostAsync(authorId, request));
        }


        [Test]
        public void DeleteImageAsync_WithInvalidPostId_ShouldThrowNotFoundException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var imageId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _postService.DeleteImageAsync(postId, imageId, authorId));
        }

        [Test]
        public async Task PublishPostAsync_WithValidData_ShouldUpdateStatus()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var post = new PostModel
            {
                PostId = postId,
                AuthorId = Guid.Parse(authorId),
                Status = "Draft",
                Title = "Test Post"
            };

            _context.Posts.Add(post);
            _context.SaveChanges();

            var request = new PublishPostRequest { Status = "Published" };

            // Act
            var result = await _postService.PublishPostAsync(postId, authorId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Published"));
            var updatedPost = _context.Posts.FirstOrDefault(p => p.PostId == postId);
            Assert.That(updatedPost.Status, Is.EqualTo("Published"));
        }

        [Test]
        public void PublishPostAsync_WithInvalidPostId_ShouldThrowNotFoundException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var request = new PublishPostRequest { Status = "Published" };

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _postService.PublishPostAsync(postId, authorId, request));
        }

        [Test]
        public void PublishPostAsync_WithUnauthorizedUser_ShouldThrowForbiddenException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var post = new PostModel
            {
                PostId = postId,
                AuthorId = Guid.NewGuid(), // Different author
                Status = "Draft",
                Title = "Unauthorized Post"
            };

            _context.Posts.Add(post);
            _context.SaveChanges();

            var request = new PublishPostRequest { Status = "Published" };

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _postService.PublishPostAsync(postId, authorId, request));
        }
    }
}
