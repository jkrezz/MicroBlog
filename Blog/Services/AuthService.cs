using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Blog.Exceptions;
using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Blog.Services;

/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken)> RegisterAsync(RegisterRequest request)
    {
        ValidateRegisterRequest(request);

        if (await _userRepository.UserExistsAsync(request.Email))
        {
            throw new ForbiddenException("Email already registered.");
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = hashedPassword,
            Role = request.Role,
            RefreshToken = string.Empty,
            RefreshTokenExpiryTime = DateTime.MinValue
        };

        var accessToken = GenerateJsonWebToken(newUser, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        newUser.RefreshToken = refreshToken.Token;
        newUser.RefreshTokenExpiryTime = refreshToken.Expiry;

        await _userRepository.AddUserAsync(newUser);

        return (accessToken, refreshToken.Token);
    }

    /// <summary>
    /// Аутентификация пользователя.
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new ForbiddenException("Invalid email or password.");
        }

        var accessToken = GenerateJsonWebToken(user, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiryTime = refreshToken.Expiry;

        await _userRepository.UpdateUserAsync(user);

        return (accessToken, refreshToken.Token);
    }

    /// <summary>
    /// Обновление токена.
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var users = await _userRepository.GetAllUsersAsync();
        var user = users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new BadRequestException("Refresh Token is invalid.");
        }

        var newAccessToken = GenerateJsonWebToken(user, TimeSpan.FromHours(2));
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken.Token;
        user.RefreshTokenExpiryTime = newRefreshToken.Expiry;
        
        await _userRepository.UpdateUserAsync(user);

        return (newAccessToken, newRefreshToken.Token);
    }

    private void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            throw new BadRequestException("All fields are required.");
        }

        if (!Regex.IsMatch(request.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        {
            throw new BadRequestException("Invalid email format.");
        }

        if (request.Role != "Author" && request.Role != "Reader")
        {
            throw new BadRequestException("Invalid role.");
        }
    }

    private string GenerateJsonWebToken(UserModel user, TimeSpan expiryDuration)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtOptions:SigningKey"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _config["JwtOptions:Issuer"],
            _config["JwtOptions:Audience"],
            claims,
            expires: DateTime.UtcNow.Add(expiryDuration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (string Token, DateTime Expiry) GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return (refreshToken, DateTime.UtcNow.AddDays(7));
    }
}
