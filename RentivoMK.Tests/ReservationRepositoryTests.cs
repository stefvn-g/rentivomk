using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Enums;
using RentivoMK.Models;
using RentivoMK.Repositories;

namespace RentivoMK.Tests;

public class ReservationRepositoryTests
{
    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private (User customer, Vehicle vehicle) SeedBaseData(AppDbContext context)
    {
        var customer = new User
        {
            FirstName = "Jane", LastName = "Doe", Email = "jane@test.com",
            PasswordHash = "hash", Role = UserRole.Customer
        };
        var vehicle = new Vehicle
        {
            Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m,
            Category = "Sedan", Status = VehicleStatus.Available, Description = "Test"
        };
        context.Users.Add(customer);
        context.Vehicles.Add(vehicle);
        context.SaveChanges();
        return (customer, vehicle);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllReservations()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_ReturnsAllReservations));
        var (customer, vehicle) = SeedBaseData(context);

        context.Reservations.AddRange(
            new Reservation { CustomerId = customer.Id, VehicleId = vehicle.Id, StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3), TotalPrice = 70m, Status = ReservationStatus.Pending },
            new Reservation { CustomerId = customer.Id, VehicleId = vehicle.Id, StartDate = DateTime.UtcNow.AddDays(5), EndDate = DateTime.UtcNow.AddDays(7), TotalPrice = 70m, Status = ReservationStatus.Approved }
        );
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsReservation_WhenExists()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsReservation_WhenExists));
        var (customer, vehicle) = SeedBaseData(context);

        var reservation = new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m, Status = ReservationStatus.Pending
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        var result = await repo.GetByIdAsync(reservation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new ReservationRepository(context);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyThatCustomersReservations()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByCustomerIdAsync_ReturnsOnlyThatCustomersReservations));
        var (customer, vehicle) = SeedBaseData(context);

        var otherCustomer = new User { FirstName = "Other", LastName = "Person", Email = "other@test.com", PasswordHash = "hash", Role = UserRole.Customer };
        context.Users.Add(otherCustomer);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            new Reservation { CustomerId = customer.Id, VehicleId = vehicle.Id, StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3), TotalPrice = 70m, Status = ReservationStatus.Pending },
            new Reservation { CustomerId = otherCustomer.Id, VehicleId = vehicle.Id, StartDate = DateTime.UtcNow.AddDays(5), EndDate = DateTime.UtcNow.AddDays(7), TotalPrice = 70m, Status = ReservationStatus.Pending }
        );
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        var result = await repo.GetByCustomerIdAsync(customer.Id);

        // Assert
        Assert.Single(result);
        Assert.All(result, r => Assert.Equal(customer.Id, r.CustomerId));
    }

    [Fact]
    public async Task GetByCustomerIdAsync_IncludesCustomerAndVehicleDetails()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByCustomerIdAsync_IncludesCustomerAndVehicleDetails));
        var (customer, vehicle) = SeedBaseData(context);

        context.Reservations.Add(new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m, Status = ReservationStatus.Pending
        });
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        var result = (await repo.GetByCustomerIdAsync(customer.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Customer);
        Assert.NotNull(result[0].Vehicle);
        Assert.Equal("Jane", result[0].Customer.FirstName);
        Assert.Equal("Toyota", result[0].Vehicle.Make);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_IncludesCustomerAndVehicleNavigationProperties()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllWithDetailsAsync_IncludesCustomerAndVehicleNavigationProperties));
        var (customer, vehicle) = SeedBaseData(context);

        context.Reservations.Add(new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m, Status = ReservationStatus.Pending
        });
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        var result = (await repo.GetAllWithDetailsAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Customer);
        Assert.NotNull(result[0].Vehicle);
        Assert.Equal("Jane", result[0].Customer.FirstName);
        Assert.Equal("Corolla", result[0].Vehicle.Model);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_ReturnsEmpty_WhenNoReservations()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllWithDetailsAsync_ReturnsEmpty_WhenNoReservations));
        var repo = new ReservationRepository(context);

        // Act
        var result = await repo.GetAllWithDetailsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_PersistsReservationToDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(AddAsync_PersistsReservationToDatabase));
        var (customer, vehicle) = SeedBaseData(context);

        var repo = new ReservationRepository(context);
        var reservation = new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(2), EndDate = DateTime.UtcNow.AddDays(5),
            TotalPrice = 105m, Status = ReservationStatus.Pending
        };

        // Act
        await repo.AddAsync(reservation);

        // Assert
        var saved = await context.Reservations.FindAsync(reservation.Id);
        Assert.NotNull(saved);
        Assert.Equal(105m, saved.TotalPrice);
        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        // Arrange
        using var context = CreateContext(nameof(UpdateAsync_PersistsStatusChange));
        var (customer, vehicle) = SeedBaseData(context);

        var reservation = new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m, Status = ReservationStatus.Pending
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);

        // Act
        reservation.Status = ReservationStatus.Approved;
        reservation.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(reservation);

        // Assert
        var updated = await context.Reservations.FindAsync(reservation.Id);
        Assert.Equal(ReservationStatus.Approved, updated!.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesReservationFromDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_RemovesReservationFromDatabase));
        var (customer, vehicle) = SeedBaseData(context);

        var reservation = new Reservation
        {
            CustomerId = customer.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 70m, Status = ReservationStatus.Pending
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var repo = new ReservationRepository(context);
        int reservationId = reservation.Id;

        // Act
        await repo.DeleteAsync(reservation);

        // Assert
        var deleted = await context.Reservations.FindAsync(reservationId);
        Assert.Null(deleted);
    }
}