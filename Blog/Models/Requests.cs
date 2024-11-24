namespace Blog.Models;

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
