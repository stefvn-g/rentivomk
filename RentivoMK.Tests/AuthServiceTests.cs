using Microsoft.Extensions.Configuration;
using Moq;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;
using RentivoMK.Services;

namespace RentivoMK.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "ThisIsATestSecretKeyThatIsLongEnough123!" },
            { "JwtSettings:Issuer", "RentivoMK" },
            { "JwtSettings:Audience", "RentivoMKUsers" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new AuthService(_userRepoMock.Object, _configuration);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("ValidPass@1");
        var user = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            PasswordHash = passwordHash,
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync("jane@test.com")).ReturnsAsync(user);

        var dto = new LoginDto { Email = "jane@test.com", Password = "ValidPass@1" };

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("jane@test.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("Jane Doe", result.FullName);
    }

    [Fact]
    public async Task LoginAsync_ThrowsException_WhenEmailNotFound()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new LoginDto { Email = "nobody@test.com", Password = "SomePass@1" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto));

        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsException_WhenPasswordIsWrong()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1");
        var user = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            PasswordHash = passwordHash,
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync("jane@test.com")).ReturnsAsync(user);

        var dto = new LoginDto { Email = "jane@test.com", Password = "WrongPass@1" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(dto));

        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsException_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            PasswordHash = "someHash",
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync("jane@test.com")).ReturnsAsync(existingUser);

        var dto = new RegisterDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@test.com",
            Password = "NewPass@1"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(dto));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_HashesPasswordAndSavesUser()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john@test.com",
            Password = "MyPass@123"
        };

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("john@test.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("John Smith", result.FullName);

        // Verify AddAsync was called with a user that has a hashed (not plaintext) password
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "john@test.com" &&
            u.PasswordHash != "MyPass@123" &&
            BCrypt.Net.BCrypt.Verify("MyPass@123", u.PasswordHash)
        )), Times.Once);
    }
}