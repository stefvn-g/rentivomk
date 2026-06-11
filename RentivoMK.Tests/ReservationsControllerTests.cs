using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RentivoMK.Controllers;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;

namespace RentivoMK.Tests;

public class ReservationsControllerTests
{
    private readonly Mock<IReservationService> _reservationServiceMock;
    private readonly ReservationsController _controller;

    public ReservationsControllerTests()
    {
        _reservationServiceMock = new Mock<IReservationService>();
        _controller = new ReservationsController(_reservationServiceMock.Object);
    }

    // Helper: sets the controller's HttpContext with a fake user
    private void SetUser(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithAllReservations()
    {
        // Arrange
        SetUser(1, "Admin");

        var reservations = new List<ReservationDto>
        {
            new ReservationDto { Id = 1, CustomerId = 1, CustomerName = "Jane Doe", VehicleId = 1, VehicleName = "Toyota Corolla (2022)", TotalPrice = 105m, Status = ReservationStatus.Pending },
            new ReservationDto { Id = 2, CustomerId = 2, CustomerName = "John Smith", VehicleId = 2, VehicleName = "BMW X5 (2023)", TotalPrice = 285m, Status = ReservationStatus.Approved }
        };

        _reservationServiceMock.Setup(s => s.GetAllReservationsAsync()).ReturnsAsync(reservations);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetMy_ReturnsOk_WithCustomerReservations()
    {
        // Arrange
        SetUser(3, "Customer");

        var myReservations = new List<ReservationDto>
        {
            new ReservationDto { Id = 1, CustomerId = 3, CustomerName = "Jane Doe", VehicleId = 1, VehicleName = "Toyota Corolla (2022)", TotalPrice = 105m, Status = ReservationStatus.Pending }
        };

        _reservationServiceMock.Setup(s => s.GetMyReservationsAsync(3)).ReturnsAsync(myReservations);

        // Act
        var result = await _controller.GetMy();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(ok.Value);
        Assert.Single(returned);
        Assert.All(returned, r => Assert.Equal(3, r.CustomerId));
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenAdminRequestsAnyReservation()
    {
        // Arrange
        SetUser(1, "Admin");

        var reservation = new ReservationDto
        {
            Id = 5,
            CustomerId = 99,
            CustomerName = "Other User",
            VehicleId = 1,
            VehicleName = "Toyota Corolla (2022)",
            TotalPrice = 70m,
            Status = ReservationStatus.Pending
        };

        _reservationServiceMock.Setup(s => s.GetReservationByIdAsync(5)).ReturnsAsync(reservation);

        // Act
        var result = await _controller.GetById(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<ReservationDto>(ok.Value);
        Assert.Equal(5, returned.Id);
    }

    [Fact]
    public async Task GetById_ReturnsForbid_WhenCustomerRequestsOtherUsersReservation()
    {
        // Arrange
        SetUser(3, "Customer"); // logged in as user 3

        var reservation = new ReservationDto
        {
            Id = 5,
            CustomerId = 99, // belongs to user 99
            CustomerName = "Other User",
            VehicleId = 1,
            VehicleName = "Toyota Corolla (2022)",
            TotalPrice = 70m,
            Status = ReservationStatus.Pending
        };

        _reservationServiceMock.Setup(s => s.GetReservationByIdAsync(5)).ReturnsAsync(reservation);

        // Act
        var result = await _controller.GetById(5);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenCustomerRequestsOwnReservation()
    {
        // Arrange
        SetUser(3, "Customer");

        var reservation = new ReservationDto
        {
            Id = 5,
            CustomerId = 3, // belongs to this customer
            CustomerName = "Jane Doe",
            VehicleId = 1,
            VehicleName = "Toyota Corolla (2022)",
            TotalPrice = 70m,
            Status = ReservationStatus.Pending
        };

        _reservationServiceMock.Setup(s => s.GetReservationByIdAsync(5)).ReturnsAsync(reservation);

        // Act
        var result = await _controller.GetById(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<ReservationDto>(ok.Value);
        Assert.Equal(5, returned.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        SetUser(1, "Admin");
        _reservationServiceMock.Setup(s => s.GetReservationByIdAsync(99)).ReturnsAsync((ReservationDto?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithCreatedReservation()
    {
        // Arrange
        SetUser(3, "Customer");

        var dto = new CreateReservationDto
        {
            VehicleId = 1,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(4)
        };

        var created = new ReservationDto
        {
            Id = 10,
            CustomerId = 3,
            CustomerName = "Jane Doe",
            VehicleId = 1,
            VehicleName = "Toyota Corolla (2022)",
            TotalPrice = 105m,
            Status = ReservationStatus.Pending
        };

        _reservationServiceMock.Setup(s => s.CreateReservationAsync(3, dto)).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), createdAt.ActionName);
        Assert.Equal(10, ((ReservationDto)createdAt.Value!).Id);
    }

    [Fact]
    public async Task Approve_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        SetUser(1, "Admin");
        _reservationServiceMock.Setup(s => s.ApproveReservationAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Approve(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _reservationServiceMock.Verify(s => s.ApproveReservationAsync(1), Times.Once);
    }

    [Fact]
    public async Task Approve_PropagatesException_WhenReservationNotPending()
    {
        // Arrange
        SetUser(1, "Admin");
        _reservationServiceMock
            .Setup(s => s.ApproveReservationAsync(1))
            .ThrowsAsync(new InvalidOperationException("Only pending reservations can be approved."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Approve(1));
    }

    [Fact]
    public async Task Reject_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        SetUser(1, "Worker");
        _reservationServiceMock.Setup(s => s.RejectReservationAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Reject(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _reservationServiceMock.Verify(s => s.RejectReservationAsync(1), Times.Once);
    }

    [Fact]
    public async Task Complete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        SetUser(1, "Worker");
        _reservationServiceMock.Setup(s => s.CompleteReservationAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Complete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _reservationServiceMock.Verify(s => s.CompleteReservationAsync(1), Times.Once);
    }

    [Fact]
    public async Task Cancel_ReturnsNoContent_WhenCustomerCancelsOwnReservation()
    {
        // Arrange
        SetUser(3, "Customer");
        _reservationServiceMock
            .Setup(s => s.CancelReservationAsync(1, 3, false))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Cancel(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _reservationServiceMock.Verify(s => s.CancelReservationAsync(1, 3, false), Times.Once);
    }

    [Fact]
    public async Task Cancel_PassesIsAdminTrue_WhenCalledByAdmin()
    {
        // Arrange
        SetUser(1, "Admin");
        _reservationServiceMock
            .Setup(s => s.CancelReservationAsync(5, 1, true))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Cancel(5);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _reservationServiceMock.Verify(s => s.CancelReservationAsync(5, 1, true), Times.Once);
    }
}
