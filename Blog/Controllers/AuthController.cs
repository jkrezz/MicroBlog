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
    private IConfiguration _config;
    private static List<UserModel> _users = new ();

    public AuthController(IConfiguration configuration)
    {
        _config = configuration;
    }

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
        if (_users.Any(u => u.Email == newUserRequest.Email))
        {
            return Forbid("Email already registered.");
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
            Role = newUserRequest.Role
        };
        _users.Add(newUser);

        // Генерация токенов
        var accessToken = GenerateJSONWebToken(newUser, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        newUser.RefreshToken = refreshToken.Token;
        newUser.RefreshTokenExpiryTime = refreshToken.Expiry;

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var user = _users.FirstOrDefault(u => u.Email == loginRequest.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
        {
            return Forbid("Invalid email or password.");
        }

        // Генерация токенов
        var accessToken = GenerateJSONWebToken(user, TimeSpan.FromHours(2));
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiryTime = refreshToken.Expiry;

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        });
    }
    
    [Authorize(Roles = "Author")]
    [HttpGet("author-only")]
    public IActionResult AuthorEndpoint()
    {
        return Ok("You have access to this endpoint because you are an Author!");
    }

    [Authorize]
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint()
    {
        return Ok("You have access to this protected endpoint!");
    }

    private string GenerateJSONWebToken(UserModel userInfo, TimeSpan expiryDuration)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtOptions:SigningKey"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
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

    private (string Token, DateTime Expiry) GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return (refreshToken, DateTime.UtcNow.AddDays(7));
    }
}
