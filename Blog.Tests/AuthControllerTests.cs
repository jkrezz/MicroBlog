using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Blog.Controllers;
using Blog.Models;

namespace Blog.Tests;

[TestFixture]
public class AuthControllerTests
{
    private AuthController _controller;
    private Mock<IConfiguration> _mockConfig;

    [SetUp]
    public void SetUp()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["JwtOptions:SigningKey"]).Returns("kdsjiujbnfiubncfixudbuidujisbdsdsads");
        _mockConfig.Setup(c => c["JwtOptions:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["JwtOptions:Audience"]).Returns("TestAudience");

        _controller = new AuthController(_mockConfig.Object);
    }

    /// <summary>
    /// Проверяет, что при регистрации пользователя, когда поля не заполнены, выдает корректную ошибку.
    /// </summary>
    [Test]
    public void Register_WithEmptyFields_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "",
            Role = ""
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("All fields are required."));
    }

    /// <summary>
    /// Проверяет, что при регистрации пользователя с помощью используемого email, выдает корректную ошибку.
    /// </summary>
    [Test]
    public void Register_WithExistingEmail_ReturnsForbidden()
    {
        // Arrange
        var existingUser = new UserModel
        {
            Email = "test@example.com",
            PasswordHash = "password",
            Role = "Reader"
        };
        AuthController.Users.Add(existingUser);

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password",
            Role = "Author"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(403));
        Assert.That(objectResult?.Value, Is.EqualTo("Email already registered."));

        // Cleanup
        AuthController.Users.Clear();
    }

    /// <summary>
    /// Проверяет, что при регистрации пользователя, когда email неверного формата, выдает корректную ошибку.
    /// </summary>
    [Test]
    public void Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "password",
            Role = "Reader"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Invalid email format."));
    }

    /// <summary>
    /// Проверяет, что при регистрации пользователя, когда указана неверная роль, выдает корректную ошибку.
    /// </summary>
    [Test]
    public void Register_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password",
            Role = "Role"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Invalid role"));
    }

    /// <summary>
    /// Проверяет, что при регистрации пользователя с корректной информацией, он добавляется в коллекцию.
    /// </summary>
    [Test]
    public void Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password",
            Role = "Author"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult?.Value, Is.Not.Null);

        // Cleanup
        AuthController.Users.Clear();
    }

    /// <summary>
    /// Проверяет, что при входе пользователя с невалидным логином, будет выдана ошибка.
    /// </summary>
    [Test]
    public void Login_WithInvalidCredentials_ReturnsForbidden()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(403));
        Assert.That(objectResult?.Value, Is.EqualTo("Invalid email or password."));
    }

    /// <summary>
    /// Проверяет, что при входе пользователя с валидным логином или паролем, он войдет в учетную запись.
    /// </summary>
    [Test]
    public void Login_WithCorrectCredentials_ReturnsOkAndTokens()
    {
        // Arrange
        var existingUser = new UserModel
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Author"
        };
        AuthController.Users.Add(existingUser);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult?.Value, Is.Not.Null);

        // Cleanup
        AuthController.Users.Clear();
    }

    /// <summary>
    /// Проверяет, что при входе пользователя с невалидным паролем, будет выдана ошибка.
    /// </summary>
    [Test]
    public void Login_WithIncorrectPassword_ReturnsForbidden()
    {
        // Arrange
        var existingUser = new UserModel
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Author"
        };
        AuthController.Users.Add(existingUser);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password1"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(403));
        Assert.That(objectResult?.Value, Is.EqualTo("Invalid email or password."));

        // Cleanup
        AuthController.Users.Clear();
    }
}