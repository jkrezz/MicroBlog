using Blog.Exceptions;
using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Services;
using Blog.Services.Interfaces;
using Minio;
using Moq;

namespace Blog.Tests
{
    [TestFixture]
    public class PostServiceTests
    {
        private Mock<IMinioRepository> _mockMinioRepository;
        private Mock<IPostRepository> _mockPostRepository;
        private Mock<IIdempotencyKeysRepository> _mockIdempotencyKeysRepository;
        private IPostService _postService;

        [SetUp]
        public void SetUp()
        {
            _mockMinioRepository = new Mock<IMinioRepository>();
            _mockPostRepository = new Mock<IPostRepository>();
            _mockIdempotencyKeysRepository = new Mock<IIdempotencyKeysRepository>();
            _postService = new PostService(
                _mockMinioRepository.Object,
                _mockPostRepository.Object,
                _mockIdempotencyKeysRepository.Object
            );
        }

        [Test]
        public async Task CreatePostAsync_WithValidData_ReturnsNewPost()
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

            _mockIdempotencyKeysRepository
                .Setup(repo => repo.Contains(idempotencyKey))
                .Returns(false);

            _mockPostRepository
                .Setup(repo => repo.AddPostAsync(It.IsAny<PostModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _postService.CreatePostAsync(authorId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(request.Title, Is.EqualTo(result.Title));
            Assert.That(request.Content, Is.EqualTo(result.Content));
            Assert.That(authorId, Is.EqualTo(result.AuthorId.ToString()));
            _mockIdempotencyKeysRepository.Verify(repo => repo.Add(idempotencyKey), Times.Once);
        }

        [Test]
        public void CreatePostAsync_WithDuplicateIdempotencyKey_ThrowsConflictException()
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

            _mockIdempotencyKeysRepository
                .Setup(repo => repo.Contains(idempotencyKey))
                .Returns(true);

            // Act & Assert
            Assert.ThrowsAsync<ConflictException>(() =>
                _postService.CreatePostAsync(authorId, request));
        }

        [Test]
        public void DeleteImageAsync_WithInvalidPostId_ThrowsNotFoundException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var imageId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();

            _mockPostRepository
                .Setup(repo => repo.GetPostByIdAsync(postId))
                .ReturnsAsync((PostModel)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(() =>
                _postService.DeleteImageAsync(postId, imageId, authorId));
        }

        [Test]
        public async Task PublishPostAsync_WithValidData_UpdatesStatus()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var request = new PublishPostRequest { Status = "Published" };

            var post = new PostModel
            {
                PostId = postId,
                AuthorId = Guid.Parse(authorId),
                Status = "Draft"
            };

            _mockPostRepository
                .Setup(repo => repo.GetPostByIdAsync(postId))
                .ReturnsAsync(post);

            // Act
            var result = await _postService.PublishPostAsync(postId, authorId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(request.Status, Is.EqualTo(result.Status));
            _mockPostRepository.Verify(repo => repo.UpdatePostAsync(post), Times.Once);
        }

        [Test]
        public void PublishPostAsync_WithInvalidPostId_ThrowsNotFoundException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var request = new PublishPostRequest { Status = "Published" };

            _mockPostRepository
                .Setup(repo => repo.GetPostByIdAsync(postId))
                .ReturnsAsync((PostModel)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(() =>
                _postService.PublishPostAsync(postId, authorId, request));
        }

        [Test]
        public void PublishPostAsync_WithUnauthorizedUser_ThrowsForbiddenException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid().ToString();
            var request = new PublishPostRequest { Status = "Published" };

            var post = new PostModel
            {
                PostId = postId,
                AuthorId = Guid.NewGuid(), // Different author
                Status = "Draft"
            };

            _mockPostRepository
                .Setup(repo => repo.GetPostByIdAsync(postId))
                .ReturnsAsync(post);

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(() =>
                _postService.PublishPostAsync(postId, authorId, request));
        }
    }
}
