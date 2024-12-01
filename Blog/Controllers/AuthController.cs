using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;
using System.Text;     
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using System.Text.RegularExpressions;

namespace Blog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    public static List<UserModel> Users = new();

    public AuthController(IConfiguration configuration)
    {
        _config = configuration;
    }

    /// <summary>
    /// Регистрирация нового пользователя.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest newUserRequest)
    {
        // Проверка на пустые значения
        if (
            string.IsNullOrWhiteSpace(newUserRequest.Email) || 
            string.IsNullOrWhiteSpace(newUserRequest.Password) ||
            string.IsNullOrWhiteSpace(newUserRequest.Role))
        {
            return BadRequest("All fields are required.");
        }
        
        // Проверка на уникальность email
        if (Users.Any(u => u.Email == newUserRequest.Email))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Email already registered.");
        }

        // Проверка на валидность email с помощью регулярного выражения
        if (!Regex.IsMatch(newUserRequest.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        {
            return BadRequest("Invalid email format.");
        }

        // Проверка на корректность роли
        if (newUserRequest.Role != "Author" && newUserRequest.Role != "Reader")
        {
            return BadRequest("Invalid role");
        }

        // Хэширование пароля
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUserRequest.Password);

        // Создание нового пользователя
        var newUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Email = newUserRequest.Email,
            PasswordHash = hashedPassword,
            Role = newUserRequest.Role,
        };

        // Генерация токенов
        var accessToken = GenerateJsonWebToken(newUser, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        newUser.RefreshToken = refreshToken.Token;
        newUser.RefreshTokenExpiryTime = refreshToken.Expiry;

        Users.Add(newUser);
        
        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        });
    }

    /// <summary>
    /// Вход пользователя.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var user = Users.FirstOrDefault(u => u.Email == loginRequest.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid email or password.");
        }

        // Генерация токенов
        var accessToken = GenerateJsonWebToken(user, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiryTime = refreshToken.Expiry;

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        });
    }
    
    /// <summary>
    /// Refresh-token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid input.");
        }

        var user = Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);
        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return BadRequest("Refresh Token is invalid or has expired.");
        }

        var newAccessToken = GenerateJsonWebToken(user, TimeSpan.FromHours(2));
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken.Token;
        user.RefreshTokenExpiryTime = newRefreshToken.Expiry;

        return Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token
        });
    }

    // Генерация JWT 
    private string GenerateJsonWebToken(UserModel userInfo, TimeSpan expiryDuration)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtOptions:SigningKey"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
            new Claim(ClaimTypes.Role, userInfo.Role),
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
    
    // Генерация Refresh
    private (string Token, DateTime Expiry) GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return (refreshToken, DateTime.UtcNow.AddDays(7));
    }
}
