using Blog.Models;

namespace Blog.Services.Interfaces;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> RegisterAsync(RegisterRequest newUserRequest); 
    Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequest loginRequest);
    Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(RefreshTokenRequest request);
}