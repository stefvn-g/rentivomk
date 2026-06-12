using Microsoft.Extensions.Caching.Memory;
using Moq;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;
using RentivoMK.Services;

namespace RentivoMK.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        _userService = new UserService(_userRepoMock.Object, cache);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Role = UserRole.Customer },
            new User { Id = 2, FirstName = "Admin", LastName = "User", Email = "admin@test.com", Role = UserRole.Admin }
        };

        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, u => u.Email == "jane@test.com");
        Assert.Contains(result, u => u.Role == UserRole.Admin);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("jane@test.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByIdAsync(99);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsException_WhenUserNotFound()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var dto = new UpdateUserDto
        {
            FirstName = "New",
            LastName = "Name",
            Email = "new@test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.UpdateUserAsync(99, dto));
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser_WhenFound()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new UpdateUserDto
        {
            FirstName = "Janet",
            LastName = "Smith",
            Email = "janet@test.com",
            Role = UserRole.Worker
        };

        // Act
        await _userService.UpdateUserAsync(1, dto);

        // Assert
        Assert.Equal("Janet", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.Equal("janet@test.com", user.Email);
        Assert.Equal(UserRole.Worker, user.Role);
        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsException_WhenUserNotFound()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.DeleteUserAsync(99));
    }

    [Fact]
    public async Task DeleteUserAsync_DeletesUser_WhenFound()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Role = UserRole.Customer
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.DeleteAsync(user)).Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteUserAsync(1);

        // Assert
        _userRepoMock.Verify(r => r.DeleteAsync(user), Times.Once);
    }
}