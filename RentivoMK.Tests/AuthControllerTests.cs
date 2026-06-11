using Microsoft.AspNetCore.Mvc;
using Moq;
using RentivoMK.Controllers;
using RentivoMK.DTOs;
using RentivoMK.Interfaces;

namespace RentivoMK.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john@test.com",
            Password = "MyPass@123"
        };

        var response = new AuthResponseDto
        {
            Token = "fake.jwt.token",
            Email = "john@test.com",
            Role = "Customer",
            FullName = "John Smith"
        };

        _authServiceMock.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(response);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("john@test.com", returned.Email);
        Assert.Equal("Customer", returned.Role);
        Assert.NotEmpty(returned.Token);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithAuthResponse()
    {
        // Arrange
        var dto = new LoginDto { Email = "jane@test.com", Password = "ValidPass@1" };

        var response = new AuthResponseDto
        {
            Token = "fake.jwt.token",
            Email = "jane@test.com",
            Role = "Customer",
            FullName = "Jane Doe"
        };

        _authServiceMock.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("jane@test.com", returned.Email);
        Assert.NotEmpty(returned.Token);
    }

    [Fact]
    public async Task Register_PropagatesException_WhenEmailAlreadyExists()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Password = "Pass@123"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(dto))
            .ThrowsAsync(new InvalidOperationException("A user with this email already exists."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Register(dto));
    }

    [Fact]
    public async Task Login_PropagatesException_WhenCredentialsAreInvalid()
    {
        // Arrange
        var dto = new LoginDto { Email = "nobody@test.com", Password = "WrongPass" };

        _authServiceMock
            .Setup(s => s.LoginAsync(dto))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Login(dto));
    }
}

