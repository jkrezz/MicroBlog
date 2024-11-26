using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Blog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    // Локальное хранилище постов
    private static readonly List<PostModel> _posts = new();
    private static readonly HashSet<string> _usedIdempotencyKeys = new();
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
    /// Добавление картинок к посту.
    /// </summary>
    [Authorize(Roles = "Author")]
    [HttpPost("{postId}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddImagesToPost([FromRoute] Guid postId, [FromForm] List<IFormFile> images)
    {
        var post = _posts.FirstOrDefault(p => p.PostId == postId);
        if (post == null)
        {
            return NotFound("Post not found.");
        }

        // Проверка прав доступа
        var authorId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (post.AuthorId.ToString() != authorId)
        {
            return Forbid("You do not have access to this post.");
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

                var objectName = $"{postId}/{Guid.NewGuid()}_{image.FileName}";

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