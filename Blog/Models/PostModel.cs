using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blog.Models;
public class PostModel
{
    /// <summary>
    /// Уникальный идентификатор поста.
    /// </summary>
    [Key]
    public Guid PostId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор автора (ссылка на пользователя).
    /// </summary>
    [Required(ErrorMessage = "AuthorId is required.")]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Уникальный ключ.
    /// </summary>
    [Required(ErrorMessage = "IdempotencyKey is required.")]
    [StringLength(50, ErrorMessage = "IdempotencyKey must be up to 50 characters.")]
    public string IdempotencyKey { get; set; }

    /// <summary>
    /// Заголовок поста.
    /// </summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
    public string Title { get; set; }

    /// <summary>
    /// Содержимое поста.
    /// </summary>
    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; }

    /// <summary>
    /// Дата создания поста.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления поста.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Статус поста ("Draft" или "Published").
    /// </summary>
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Draft|Published)$", ErrorMessage = "Status must be 'Draft' or 'Published'.")]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Коллекция изображений, связанных с постом.
    /// </summary>
    public List<ImageModel> Images { get; set; } = new List<ImageModel>();
}