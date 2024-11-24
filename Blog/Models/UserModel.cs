using System.ComponentModel.DataAnnotations;
namespace Blog.Models;

public class UserModel
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    /// <summary>
    /// Хэш пароля пользователя.
    /// </summary>
    [Required(ErrorMessage = "Password hash is required.")]
    public string PasswordHash { get; set; }

    /// <summary>
    /// Роль пользователя: Author или Reader.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Author|Reader)$", ErrorMessage = "Role must be 'Author' or 'Reader'.")]
    public string Role { get; set; }

    /// <summary>
    /// Токен обновления (Refresh Token).
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// Время истечения токена обновления.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}