using Microsoft.AspNetCore.Mvc;
using Moq;
using RentivoMK.Controllers;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;

namespace RentivoMK.Tests;

public class VehiclesControllerTests
{
    private readonly Mock<IVehicleService> _vehicleServiceMock;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _vehicleServiceMock = new Mock<IVehicleService>();
        _controller = new VehiclesController(_vehicleServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithAllVehicles()
    {
        // Arrange
        var vehicles = new List<VehicleDto>
        {
            new VehicleDto { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" },
            new VehicleDto { Id = 2, Make = "BMW", Model = "X5", Year = 2023, PricePerDay = 95m, Category = "SUV", Status = VehicleStatus.Available, Description = "Test" }
        };

        _vehicleServiceMock.Setup(s => s.GetAllVehiclesAsync()).ReturnsAsync(vehicles);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<VehicleDto>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetAvailable_ReturnsOk_WithAvailableVehiclesOnly()
    {
        // Arrange
        var available = new List<VehicleDto>
        {
            new VehicleDto { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" }
        };

        _vehicleServiceMock.Setup(s => s.GetAvailableVehiclesAsync()).ReturnsAsync(available);

        // Act
        var result = await _controller.GetAvailable();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<VehicleDto>>(ok.Value);
        Assert.Single(returned);
        Assert.All(returned, v => Assert.Equal(VehicleStatus.Available, v.Status));
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenVehicleExists()
    {
        // Arrange
        var vehicle = new VehicleDto { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" };

        _vehicleServiceMock.Setup(s => s.GetVehicleByIdAsync(1)).ReturnsAsync(vehicle);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<VehicleDto>(ok.Value);
        Assert.Equal("Toyota", returned.Make);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenVehicleDoesNotExist()
    {
        // Arrange
        _vehicleServiceMock.Setup(s => s.GetVehicleByIdAsync(99)).ReturnsAsync((VehicleDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithCreatedVehicle()
    {
        // Arrange
        var dto = new CreateVehicleDto
        {
            Make = "Ford",
            Model = "Focus",
            Year = 2021,
            PricePerDay = 40m,
            Category = "Hatchback",
            Description = "Compact car"
        };

        var created = new VehicleDto
        {
            Id = 5,
            Make = "Ford",
            Model = "Focus",
            Year = 2021,
            PricePerDay = 40m,
            Category = "Hatchback",
            Status = VehicleStatus.Available,
            Description = "Compact car"
        };

        _vehicleServiceMock.Setup(s => s.CreateVehicleAsync(dto)).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), createdAt.ActionName);
        Assert.Equal(5, ((VehicleDto)createdAt.Value!).Id);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var dto = new UpdateVehicleDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2022,
            PricePerDay = 38m,
            Category = "Sedan",
            Description = "Updated",
            Status = VehicleStatus.Available
        };

        _vehicleServiceMock.Setup(s => s.UpdateVehicleAsync(1, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _vehicleServiceMock.Verify(s => s.UpdateVehicleAsync(1, dto), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _vehicleServiceMock.Setup(s => s.DeleteVehicleAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _vehicleServiceMock.Verify(s => s.DeleteVehicleAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_PropagatesException_WhenActiveReservationsExist()
    {
        // Arrange
        _vehicleServiceMock
            .Setup(s => s.DeleteVehicleAsync(1))
            .ThrowsAsync(new InvalidOperationException("Cannot delete a vehicle with active (Pending or Approved) reservations."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Delete(1));
    }
}