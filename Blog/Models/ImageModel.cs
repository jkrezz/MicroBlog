using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Blog.Models;

/// <summary>
/// Модель изображения, связанного с постом.
/// </summary>
public class ImageModel
{
    /// <summary>
    /// Уникальный идентификатор изображения.
    /// </summary>
    public Guid ImageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор поста, к которому привязано изображение.
    /// </summary>
    public Guid PostId { get; set; }

    /// <summary>
    /// URL изображения.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Дата создания изображения.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}