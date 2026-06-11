using Microsoft.AspNetCore.Mvc;
using Moq;
using RentivoMK.Controllers;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;

namespace RentivoMK.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithListOfUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Role = UserRole.Customer },
            new UserDto { Id = 2, FirstName = "Admin", LastName = "User", Email = "admin@test.com", Role = UserRole.Admin }
        };

        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<UserDto>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var user = new UserDto { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Role = UserRole.Customer };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<UserDto>(ok.Value);
        Assert.Equal("Jane", returned.FirstName);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetUserByIdAsync(99)).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var dto = new UpdateUserDto { FirstName = "Janet", LastName = "Smith", Email = "janet@test.com", Role = UserRole.Worker };

        _userServiceMock.Setup(s => s.UpdateUserAsync(1, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _userServiceMock.Verify(s => s.UpdateUserAsync(1, dto), Times.Once);
    }

    [Fact]
    public async Task Update_PropagatesException_WhenUserNotFound()
    {
        // Arrange
        var dto = new UpdateUserDto { FirstName = "X", LastName = "Y", Email = "x@test.com" };

        _userServiceMock
            .Setup(s => s.UpdateUserAsync(99, dto))
            .ThrowsAsync(new KeyNotFoundException("User with ID 99 not found."));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(99, dto));
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _userServiceMock.Setup(s => s.DeleteUserAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _userServiceMock.Verify(s => s.DeleteUserAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_PropagatesException_WhenUserNotFound()
    {
        // Arrange
        _userServiceMock
            .Setup(s => s.DeleteUserAsync(99))
            .ThrowsAsync(new KeyNotFoundException("User with ID 99 not found."));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Delete(99));
    }
}
