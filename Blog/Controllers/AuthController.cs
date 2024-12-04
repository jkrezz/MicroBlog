using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;
using System.Text;     
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using System.Text.RegularExpressions;
using Blog.Services;
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
    public IActionResult Register([FromBody] RegisterRequest newUserRequest)
    {
        var tokens = _authService.Register(newUserRequest);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        var tokens = _authService.Login(loginRequest);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokens = _authService.RefreshToken(request);
        return Ok(new
        {
            tokens.AccessToken,
            tokens.RefreshToken
        });
    }
}

