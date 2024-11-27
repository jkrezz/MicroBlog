using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    // Локальное хранилище постов
    private static readonly List<PostModel> Posts = new();
    private static readonly HashSet<string> UsedIdempotencyKeys = new();
    private readonly IMinioClient _minioClient;

    public PostsController(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

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

        if (UsedIdempotencyKeys.Contains(postRequest.IdempotencyKey))
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
        Posts.Add(newPost);
        UsedIdempotencyKeys.Add(postRequest.IdempotencyKey);

        return CreatedAtAction(nameof(GetPostById), new { id = newPost.PostId }, newPost);
    }

    /// <summary>
    /// Добавление картинок к посту.
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpPost("{postId}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddImagesToPost([FromRoute] Guid postId, [FromForm] List<IFormFile>? images)
    {
        var post = Posts.FirstOrDefault(p => p.PostId == postId);

        if (post == null)
        {
            return NotFound("Post not found.");
        }

        // Проверка прав доступа
        var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (post.AuthorId.ToString() != authorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
        }

        if (images == null || !images.Any())
        {
            return BadRequest("Image file is required.");
        }

        var bucketName = "post-images";

        var uploadedImages = new List<ImageModel>();

        try
        {
            // Проверка существования бакета
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);

            if (!await _minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false))
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs).ConfigureAwait(false);
            }

            foreach (var image in images)
            {
                if (image.Length == 0) continue;
                var id = Guid.NewGuid();
                var objectName = $"{postId}/{id}";

                // Загрузка файла
                using (var stream = image.OpenReadStream())
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(stream)
                        .WithObjectSize(image.Length)
                        .WithContentType(image.ContentType);

                    await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }

                // Создание ссылки
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(60 * 60);

                var imageUrl = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

                // Добавление изображения в список
                var newImage = new ImageModel
                {
                    ImageId = id,
                    PostId = postId,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };
                post.Images.Add(newImage);

                uploadedImages.Add(newImage);
            }

            return Created("", new { UploadedImages = uploadedImages });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to upload images. Error: {ex.Message}");
        }
    }


    /// <summary>
    /// Редактирование поста по id.
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpPut("{postId}")]
    public IActionResult EditPost([FromRoute] Guid postId, [FromBody] UpdatePost updatePost)
    {
        var post = Posts.FirstOrDefault(p => p.PostId == postId);

        if (post == null)
        {
            return NotFound("Post not found.");
        }

        // Проверка прав доступа
        var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (post.AuthorId.ToString() != authorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
        }

        if (string.IsNullOrWhiteSpace(updatePost.Title) ||
            string.IsNullOrWhiteSpace(updatePost.Content))
        {
            return BadRequest("All fields are required.");
        }

        post.Title = updatePost.Title;
        post.Content = updatePost.Content;
        post.UpdatedAt = DateTime.UtcNow;


        return Ok(new
        {
            Message = "Post successfully updated.",
            UpdatedPost = post
        });
    }

    /// <summary>
    /// Удаление картинки из MinIO
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpDelete("{postId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImages([FromRoute] Guid postId, [FromRoute] Guid imageId)
    {
        var post = Posts.FirstOrDefault(p => p.PostId == postId);

        if (post == null)
        {
            return NotFound("Post not found.");
        }

        // Проверка прав доступа
        var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (post.AuthorId.ToString() != authorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
        }

        var image = post.Images.FirstOrDefault(p => p.ImageId == imageId);

        if (image == null)
        {
            return NotFound("Image not found.");
        }

        try
        {
            var bucketName = "post-images";

            // Удаление картинки из MinIO
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject($"{postId}/{imageId}");

            await _minioClient.RemoveObjectAsync(removeObjectArgs).ConfigureAwait(false);

            // Удаление картинки из базы данных (имитация)
            post.Images.Remove(image);

            return Ok(new { Message = "Image successfully deleted." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to delete image. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Опубликовать пост
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpPatch("{postId}/status")]
    public IActionResult PublishPost([FromRoute] Guid postId, [FromBody] PublishPostRequest request)
    {
        var post = Posts.FirstOrDefault(p => p.PostId == postId);

        if (post == null)
        {
            return NotFound("Post not found.");
        }

        // Проверка прав доступа
        var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (post.AuthorId.ToString() != authorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
        }

        var validStatuses = new[] { "Published", "Draft" };

        if (string.IsNullOrEmpty(request.Status) || !validStatuses.Contains(request.Status))
        {
            return BadRequest($"Invalid status value.");
        }

        post.Status = request.Status;

        post.UpdatedAt = DateTime.UtcNow;

        return Ok(new
        {
            Message = "Post successfully published.",
            UpdatedPost = post
        });
    }

    /// <summary>
    /// Вернуть все посты
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult GetPosts()
    {
        // Получение роли пользователя
        var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userRole) || string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Unable to determine user role or ID.");
        }

        // Автору вернуть все посты
        if (userRole == "Author")
        {
            var authorPosts = Posts.Where(p => p.AuthorId.ToString() == userId).ToList();

            return Ok(new
            {
                Message = "List of posts for the author.",
                Posts = authorPosts
            });
        }

        // Читателям доступны посты со статусом "Published"
        if (userRole == "Reader")
        {
            var publishedPosts = Posts.Where(p => p.Status == "Published").ToList();

            return Ok(new
            {
                Message = "List of posts published.",
                Posts = publishedPosts
            });
        }

        return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
    }

    /// <summary>
    /// Получение поста по его идентификатору.
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpGet("{id}")]
    public IActionResult GetPostById(Guid id)
    {
        var post = Posts.FirstOrDefault(p => p.PostId == id);

        if (post == null)
        {
            return NotFound("Post not found.");
        }

        return Ok(post);
    }
}