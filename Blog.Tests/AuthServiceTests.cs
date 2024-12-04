using Moq;
using Blog.Services;
using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Blog.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IConfiguration> _mockConfig;
    private AuthService _authService;

    [SetUp]
    public void SetUp()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(c => c["JwtOptions:SigningKey"]).Returns("isodsudhbuifhgbdsuifhbsdiudfsahiudssadas");
        _mockConfig.Setup(c => c["JwtOptions:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["JwtOptions:Audience"]).Returns("TestAudience");

        _authService = new AuthService(_mockUserRepository.Object, _mockConfig.Object);
    }

    [Test]
    public void Register_WithEmptyFields_ThrowsBadRequestException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "",
            Role = ""
        };

        // Act & Assert
        var ex = Assert.Throws<BadRequestException>(() => _authService.Register(request));
        Assert.That(ex.Message, Is.EqualTo("All fields are required."));
    }

    [Test]
    public void Register_WithExistingEmail_ThrowsForbiddenException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@gmail.com",
            Password = "password",
            Role = "Author"
        };

        _mockUserRepository.Setup(r => r.UserExists(request.Email)).Returns(true);

        // Act & Assert
        var ex = Assert.Throws<ForbiddenException>(() => _authService.Register(request));
        Assert.That(ex.Message, Is.EqualTo("Email already registered."));
    }

    [Test]
    public void Register_WithValidData_ReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@gmail.com",
            Password = "password",
            Role = "Author"
        };

        _mockUserRepository.Setup(r => r.UserExists(request.Email)).Returns(false);

        // Act
        var result = _authService.Register(request);

        // Assert
        Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
        _mockUserRepository.Verify(r => r.AddUser(It.IsAny<UserModel>()), Times.Once);
    }

    [Test]
    public void Login_WithInvalidCredentials_ThrowsForbiddenException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@gmail.com",
            Password = "wrongpassword"
        };

        _mockUserRepository.Setup(r => r.GetUserByEmail(request.Email))
            .Returns(new UserModel
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
            });

        // Act & Assert
        var ex = Assert.Throws<ForbiddenException>(() => _authService.Login(request));
        Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public void RefreshToken_WithInvalidToken_ThrowsBadRequestException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalidToken"
        };

        _mockUserRepository.Setup(r => r.GetAllUsers()).Returns(new List<UserModel>());

        // Act & Assert
        var ex = Assert.Throws<BadRequestException>(() => _authService.RefreshToken(request));
        Assert.That(ex.Message, Is.EqualTo("Refresh Token is invalid."));
    }

    [Test]
    public void RefreshToken_WithExpiredToken_ThrowsBadRequestException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "Token"
        };

        _mockUserRepository.Setup(r => r.GetAllUsers()).Returns(new List<UserModel>
        {
            new UserModel
            {
                RefreshToken = "Token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1)
            }
        });

        // Act & Assert
        var ex = Assert.Throws<BadRequestException>(() => _authService.RefreshToken(request));
        Assert.That(ex.Message, Is.EqualTo("Refresh Token is invalid."));
    }
}
