using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Enums;
using RentivoMK.Models;
using RentivoMK.Repositories;

namespace RentivoMK.Tests;

public class VehicleRepositoryTests
{
    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllVehicles()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_ReturnsAllVehicles));
        context.Vehicles.AddRange(
            new Vehicle { Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" },
            new Vehicle { Make = "BMW", Model = "X5", Year = 2023, PricePerDay = 95m, Category = "SUV", Status = VehicleStatus.Rented, Description = "Test" }
        );
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsVehicle_WhenExists()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsVehicle_WhenExists));
        var vehicle = new Vehicle { Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);

        // Act
        var result = await repo.GetByIdAsync(vehicle.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Toyota", result.Make);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new VehicleRepository(context);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_ReturnsOnlyAvailableVehicles()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAvailableVehiclesAsync_ReturnsOnlyAvailableVehicles));
        context.Vehicles.AddRange(
            new Vehicle { Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" },
            new Vehicle { Make = "BMW", Model = "X5", Year = 2023, PricePerDay = 95m, Category = "SUV", Status = VehicleStatus.Rented, Description = "Test" },
            new Vehicle { Make = "Mercedes", Model = "C-Class", Year = 2023, PricePerDay = 85m, Category = "Sedan", Status = VehicleStatus.Maintenance, Description = "Test" }
        );
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);

        // Act
        var result = await repo.GetAvailableVehiclesAsync();

        // Assert
        Assert.Single(result);
        Assert.All(result, v => Assert.Equal(VehicleStatus.Available, v.Status));
        Assert.Contains(result, v => v.Make == "Toyota");
    }

    [Fact]
    public async Task AddAsync_PersistsVehicleToDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(AddAsync_PersistsVehicleToDatabase));
        var repo = new VehicleRepository(context);

        var vehicle = new Vehicle { Make = "Ford", Model = "Focus", Year = 2021, PricePerDay = 40m, Category = "Hatchback", Status = VehicleStatus.Available, Description = "Compact" };

        // Act
        await repo.AddAsync(vehicle);

        // Assert
        var saved = await context.Vehicles.FirstOrDefaultAsync(v => v.Make == "Ford");
        Assert.NotNull(saved);
        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        // Arrange
        using var context = CreateContext(nameof(UpdateAsync_PersistsChanges));
        var vehicle = new Vehicle { Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);

        // Act
        vehicle.PricePerDay = 45m;
        vehicle.Status = VehicleStatus.Rented;
        await repo.UpdateAsync(vehicle);

        // Assert
        var updated = await context.Vehicles.FindAsync(vehicle.Id);
        Assert.Equal(45m, updated!.PricePerDay);
        Assert.Equal(VehicleStatus.Rented, updated.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesVehicleFromDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_RemovesVehicleFromDatabase));
        var vehicle = new Vehicle { Make = "Toyota", Model = "Corolla", Year = 2022, PricePerDay = 35m, Category = "Sedan", Status = VehicleStatus.Available, Description = "Test" };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        int vehicleId = vehicle.Id;

        // Act
        await repo.DeleteAsync(vehicle);

        // Assert
        var deleted = await context.Vehicles.FindAsync(vehicleId);
        Assert.Null(deleted);
    }
}