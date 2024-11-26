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
    [Key]
    public Guid ImageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор поста, к которому привязано изображение.
    /// </summary>
    [Required(ErrorMessage = "PostId is required.")]
    public Guid PostId { get; set; }

    /// <summary>
    /// URL изображения.
    /// </summary>
    [Required(ErrorMessage = "ImageUrl is required.")]
    [Url(ErrorMessage = "Invalid URL format.")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Дата создания изображения.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}