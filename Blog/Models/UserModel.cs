using System.ComponentModel.DataAnnotations;
namespace Blog.Models;

/// <summary>
/// Модель пользователя.
/// </summary>
public class UserModel
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Хэш пароля пользователя.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Роль пользователя: Author или Reader.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Токен обновления (Refresh Token).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Время истечения токена обновления.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}