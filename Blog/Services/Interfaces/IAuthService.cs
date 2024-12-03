using Blog.Models;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Services.Interfaces;


public interface IAuthService
{
    (string AccessToken, string RefreshToken) Register(RegisterRequest newUserRequest); 
    (string AccessToken, string RefreshToken) Login(LoginRequest loginRequest);
    (string AccessToken, string RefreshToken) RefreshToken(RefreshTokenRequest request);
}