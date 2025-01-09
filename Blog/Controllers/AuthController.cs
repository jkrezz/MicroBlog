using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Blog.Services.Interfaces;

namespace Blog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest newUserRequest)
    {
        var tokens = await _authService.RegisterAsync(newUserRequest);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var tokens = await _authService.LoginAsync(loginRequest);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokens = await _authService.RefreshTokenAsync(request);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }
}