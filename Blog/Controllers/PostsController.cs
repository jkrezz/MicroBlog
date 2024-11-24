using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
namespace Blog.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
    {
        // Локальное хранилище постов (заменить на базу данных в реальном приложении)
        private static readonly List<PostModel> _posts = new();
        private static readonly HashSet<string> _usedIdempotencyKeys = new();

        /// <summary>
        /// Создание нового поста.
        /// </summary>
        [Authorize(Roles = "Author")]
        [HttpPost]
        public IActionResult CreatePost([FromBody] CreatePostRequest postRequest)
        {
            if (string.IsNullOrWhiteSpace(postRequest.IdempotencyKey) ||
                string.IsNullOrWhiteSpace(postRequest.Title) ||
                string.IsNullOrWhiteSpace(postRequest.Content))
            {
                return BadRequest("All fields are required.");
            }
            
            if (_usedIdempotencyKeys.Contains(postRequest.IdempotencyKey))
            {
                return Conflict("IdempotencyKey has already been used.");
            }

            // Извлечение идентификатора текущего пользователя
            var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(authorId))
            {
                return Unauthorized("User is not authenticated.");
            }

            // Создание нового поста
            var newPost = new PostModel
            {
                PostId = Guid.NewGuid(),
                AuthorId = Guid.Parse(authorId),
                IdempotencyKey = postRequest.IdempotencyKey,
                Title = postRequest.Title,
                Content = postRequest.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "Draft"
            };

            // Сохранение поста и ключа идемпотентности
            _posts.Add(newPost);
            _usedIdempotencyKeys.Add(postRequest.IdempotencyKey);

            return CreatedAtAction(nameof(GetPostById), new { id = newPost.PostId }, newPost);
        }

        /// <summary>
        /// Получение поста по его идентификатору.
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetPostById(Guid id)
        {
            var post = _posts.FirstOrDefault(p => p.PostId == id);
            if (post == null)
            {
                return NotFound("Post not found.");
            }

            return Ok(post);
        }
    }