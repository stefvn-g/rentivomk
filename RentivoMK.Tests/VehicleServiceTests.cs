using Microsoft.Extensions.Caching.Memory;
using Moq;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;
using RentivoMK.Services;

namespace RentivoMK.Tests;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly Mock<IReservationRepository> _reservationRepoMock;
    private readonly VehicleService _vehicleService;

    public VehicleServiceTests()
    {
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _reservationRepoMock = new Mock<IReservationRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        _vehicleService = new VehicleService(_vehicleRepoMock.Object, _reservationRepoMock.Object, cache);
    }

    [Fact]
    public async Task GetAllVehiclesAsync_ReturnsAllVehicles()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new Vehicle { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" },
            new Vehicle { Id = 2, Make = "BMW", Model = "X5", Year = 2023, PricePerDay = 95m, Category = "SUV", Status = VehicleStatus.Available, Description = "Test" }
        };

        _vehicleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(vehicles);

        // Act
        var result = await _vehicleService.GetAllVehiclesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, v => v.Make == "Toyota");
        Assert.Contains(result, v => v.Make == "BMW");
    }

    [Fact]
    public async Task GetVehicleByIdAsync_ReturnsVehicle_WhenExists()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = 1,
            Make = "Toyota",
            Model = "Corolla",
            Year = 2022,
            PricePerDay = 35m,
            Category = "Sedan",
            Status = VehicleStatus.Available,
            Description = "Reliable sedan"
        };

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);

        // Act
        var result = await _vehicleService.GetVehicleByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Toyota", result.Make);
        Assert.Equal("Corolla", result.Model);
    }

    [Fact]
    public async Task GetVehicleByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _vehicleService.GetVehicleByIdAsync(99);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateVehicleAsync_CreatesAndReturnsVehicle()
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

        _vehicleRepoMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

        // Act
        var result = await _vehicleService.CreateVehicleAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ford", result.Make);
        Assert.Equal("Focus", result.Model);
        Assert.Equal(40m, result.PricePerDay);
        Assert.Equal(VehicleStatus.Available, result.Status);
        _vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Once);
    }

    [Fact]
    public async Task DeleteVehicleAsync_ThrowsException_WhenActiveReservationsExist()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = 1,
            Make = "Toyota",
            Model = "Corolla",
            Year = 2022,
            PricePerDay = 35m,
            Category = "Sedan",
            Status = VehicleStatus.Available,
            Description = "Test"
        };

        var activeReservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                VehicleId = 1,
                CustomerId = 1,
                Status = ReservationStatus.Approved,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
                TotalPrice = 70m,
                Customer = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com" },
                Vehicle = vehicle
            }
        };

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(activeReservations);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _vehicleService.DeleteVehicleAsync(1));

        Assert.Contains("active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteVehicleAsync_Succeeds_WhenNoActiveReservations()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = 1,
            Make = "Toyota",
            Model = "Corolla",
            Year = 2022,
            PricePerDay = 35m,
            Category = "Sedan",
            Status = VehicleStatus.Available,
            Description = "Test"
        };

        var completedReservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                VehicleId = 1,
                CustomerId = 1,
                Status = ReservationStatus.Completed,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(-2),
                TotalPrice = 70m,
                Customer = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com" },
                Vehicle = vehicle
            }
        };

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(completedReservations);
        _vehicleRepoMock.Setup(r => r.DeleteAsync(vehicle)).Returns(Task.CompletedTask);

        // Act
        await _vehicleService.DeleteVehicleAsync(1);

        // Assert
        _vehicleRepoMock.Verify(r => r.DeleteAsync(vehicle), Times.Once);
    }
}