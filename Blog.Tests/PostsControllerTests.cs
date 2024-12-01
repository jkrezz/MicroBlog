using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Blog.Controllers;
using Blog.Models;
using System.Security.Claims;
using Minio;

namespace Blog.Tests
{
    [TestFixture]
    
    public class PostsControllerTests
    {
        
        private Mock<IMinioClient> _mockMinioClient;
        private PostsController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockMinioClient = new Mock<IMinioClient>();
            _controller = new PostsController(_mockMinioClient.Object);
        }

        /// <summary>
        /// Проверяет, что при вводе корректных данных, будет создан новый пост.
        /// </summary>
        [Test]
        public void CreatePost_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreatePostRequest
            {
                IdempotencyKey = "unique-key-123",
                Title = "Test Post",
                Content = "This is a test post."
            };

            var userClaims = new List<Claim>
            {
                new (ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var userContext = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userContext }
            };

            // Act
            var result = _controller.CreatePost(request);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
            var createdAtResult = result as CreatedAtActionResult;
            Assert.That(createdAtResult?.StatusCode, Is.EqualTo(201));
        }

        /// <summary>
        /// Проверяет, что при не заполнении всех полей, будет выдана ошибка.
        /// </summary>
        [Test]
        public void CreatePost_WithMissingFields_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreatePostRequest
            {
                IdempotencyKey = "unique-key-123",
                Title = "",
                Content = "This is a test post."
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var userContext = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userContext }
            };

            // Act
            var result = _controller.CreatePost(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult?.Value, Is.EqualTo("All fields are required."));
        }

        /// <summary>
        /// Проверяет, что при использовании уже существующего IdempotencyKey, будет ошибка.
        /// </summary>
        [Test]
        public void CreatePost_WithUsedIdempotencyKey_ReturnsConflict()
        {
            // Arrange
            var request = new CreatePostRequest
            {
                IdempotencyKey = "used-key",
                Title = "Test Post",
                Content = "This is a test post."
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var userContext = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userContext }
            };
            
            PostsController.UsedIdempotencyKeys.Add("used-key");

            // Act
            var result = _controller.CreatePost(request);

            // Assert
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
            var conflictResult = result as ConflictObjectResult;
            Assert.That(conflictResult?.Value, Is.EqualTo("IdempotencyKey has already been used."));
        }

        /// <summary>
        /// Проверяет, что пользователь неаунтифицирован, выдавая ошибку.
        /// </summary>
        [Test]
        public void CreatePost_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CreatePostRequest
            {
                IdempotencyKey = "unique-key-123",
                Title = "Test Post",
                Content = "This is a test post."
            };

            var userClaims = new List<Claim>();
            var userContext = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userContext }
            };

            // Act
            var result = _controller.CreatePost(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.That(unauthorizedResult?.Value, Is.EqualTo("User is not authenticated."));
        }

        /// <summary>
        /// Проверяет, что картинка успешно добавлена к посту.
        /// </summary>
        [Test]
        public async Task AddImagesToPost_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var post = new PostModel
            {
                PostId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Title = "Test Post",
                Content = "Test Content",
                IdempotencyKey = "key123",
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Images = new List<ImageModel>()
            };

            PostsController.Posts.Add(post);

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, post.AuthorId.ToString())
            };
            var userContext = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userContext }
            };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

            var files = new List<IFormFile> { fileMock.Object };

            // Act
            var result = await _controller.AddImagesToPost(post.PostId, files);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            var createdResult = result as CreatedResult;
            Assert.That(createdResult?.StatusCode, Is.EqualTo(201));
        }
    }
}
