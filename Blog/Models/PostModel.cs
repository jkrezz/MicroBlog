using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blog.Models;

/// <summary>
/// Модель поста.
/// </summary>
public class PostModel
{
    /// <summary>
    /// Идентификатор поста.
    /// </summary>
    public Guid PostId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор автора.
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Уникальный ключ.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Заголовок поста.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Содержимое поста.
    /// </summary>
    public string? Content { get; set; }

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
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Коллекция изображений, связанных с постом.
    /// </summary>
    public List<ImageModel> Images { get; set; } = new();
}