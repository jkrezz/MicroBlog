namespace Blog.Models;

public class RegisterRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public class CreatePostRequest
{
    public string? IdempotencyKey { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}

public class UpdatePost
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}

public class PublishPostRequest
{
    public string? Status { get; set; }
}