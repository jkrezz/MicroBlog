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
        try
        {
            var tokens = _authService.Register(newUserRequest);
            return Ok(new
            {
                tokens.AccessToken,
                tokens.RefreshToken
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var tokens = _authService.Login(loginRequest);
            return Ok(new
            {
                tokens.AccessToken,
                tokens.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var tokens = _authService.RefreshToken(request);
            return Ok(new
            {
                tokens.AccessToken,
                tokens.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

