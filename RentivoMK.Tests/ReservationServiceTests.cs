using Moq;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;
using RentivoMK.Services;

namespace RentivoMK.Tests;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepoMock;
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly ReservationService _reservationService;

    public ReservationServiceTests()
    {
        _reservationRepoMock = new Mock<IReservationRepository>();
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _reservationService = new ReservationService(_reservationRepoMock.Object, _vehicleRepoMock.Object);
    }

    [Fact]
    public async Task CreateReservationAsync_CalculatesTotalPriceCorrectly()
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

        var dto = new CreateReservationDto
        {
            VehicleId = 1,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(4) // 3 days
        };

        var customer = new User
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Role = UserRole.Customer
        };

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);

        _reservationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Reservation>()))
            .Callback<Reservation>(r => r.Id = 1)
            .Returns(Task.CompletedTask);

        _reservationRepoMock
            .SetupSequence(r => r.GetAllWithDetailsAsync())
            .ReturnsAsync(new List<Reservation>()) 
            .ReturnsAsync(new List<Reservation>     // reload after save
            {
            new Reservation
            {
                Id = 1,
                CustomerId = 1,
                Customer = customer,
                VehicleId = 1,
                Vehicle = vehicle,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalPrice = 35m * 3,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
            });

        // Act
        var result = await _reservationService.CreateReservationAsync(1, dto);

        // Assert
        Assert.Equal(105m, result.TotalPrice);
    }

    [Fact]
    public async Task CreateReservationAsync_ThrowsException_WhenVehicleNotAvailable()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = 1,
            Make = "BMW",
            Model = "X5",
            Year = 2023,
            PricePerDay = 95m,
            Category = "SUV",
            Status = VehicleStatus.Rented,
            Description = "Test"
        };

        var dto = new CreateReservationDto
        {
            VehicleId = 1,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(3)
        };

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _reservationService.CreateReservationAsync(1, dto));

        Assert.Contains("not available", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveReservationAsync_SetsStatusToApproved_AndVehicleToRented()
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

        var reservation = new Reservation
        {
            Id = 1,
            CustomerId = 1,
            VehicleId = 1,
            Status = ReservationStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _vehicleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

        // Act
        await _reservationService.ApproveReservationAsync(1);

        // Assert
        Assert.Equal(ReservationStatus.Approved, reservation.Status);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
        _reservationRepoMock.Verify(r => r.UpdateAsync(reservation), Times.Once);
        _vehicleRepoMock.Verify(r => r.UpdateAsync(vehicle), Times.Once);
    }

    [Fact]
    public async Task RejectReservationAsync_SetsStatusToRejected_AndVehicleToAvailable()
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

        var reservation = new Reservation
        {
            Id = 1,
            CustomerId = 1,
            VehicleId = 1,
            Status = ReservationStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _vehicleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

        // Act
        await _reservationService.RejectReservationAsync(1);

        // Assert
        Assert.Equal(ReservationStatus.Rejected, reservation.Status);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        _reservationRepoMock.Verify(r => r.UpdateAsync(reservation), Times.Once);
        _vehicleRepoMock.Verify(r => r.UpdateAsync(vehicle), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_ThrowsException_WhenUserIsNotOwnerOrAdmin()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            CustomerId = 5, // owned by user 5
            VehicleId = 1,
            Status = ReservationStatus.Pending,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert — user 99 is not the owner and not admin
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _reservationService.CancelReservationAsync(1, requestingUserId: 99, isAdmin: false));

        Assert.Contains("not authorized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}