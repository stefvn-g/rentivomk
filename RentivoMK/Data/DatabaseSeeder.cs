using RentivoMK.Enums;
using RentivoMK.Models;

namespace RentivoMK.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.Users.Any() || context.Vehicles.Any())
            return;

        // Admin User
        var admin = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@rentivomk.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        // 5 Sample Vehicles
        var vehicles = new List<Vehicle>
        {
            new Vehicle
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2022,
                PricePerDay = 35.00m,
                Category = "Sedan",
                Status = VehicleStatus.Available,
                Description = "Reliable and fuel-efficient sedan.",
                CreatedAt = DateTime.UtcNow
            },
            new Vehicle
            {
                Make = "BMW",
                Model = "X5",
                Year = 2023,
                PricePerDay = 95.00m,
                Category = "SUV",
                Status = VehicleStatus.Available,
                Description = "Luxury SUV with premium features.",
                CreatedAt = DateTime.UtcNow
            },
            new Vehicle
            {
                Make = "Volkswagen",
                Model = "Golf",
                Year = 2021,
                PricePerDay = 30.00m,
                Category = "Hatchback",
                Status = VehicleStatus.Available,
                Description = "Compact and easy to drive.",
                CreatedAt = DateTime.UtcNow
            },
            new Vehicle
            {
                Make = "Mercedes-Benz",
                Model = "C-Class",
                Year = 2023,
                PricePerDay = 85.00m,
                Category = "Sedan",
                Status = VehicleStatus.Maintenance,
                Description = "Premium sedan currently under maintenance.",
                CreatedAt = DateTime.UtcNow
            },
            new Vehicle
            {
                Make = "Ford",
                Model = "Transit",
                Year = 2020,
                PricePerDay = 55.00m,
                Category = "Van",
                Status = VehicleStatus.Available,
                Description = "Spacious van ideal for group travel.",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync();

        // Sample Customer for Reservations
        var customer = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(customer);
        await context.SaveChangesAsync();

        // 2 Sample Reservations
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                CustomerId = customer.Id,
                VehicleId = vehicles[0].Id,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(4),
                TotalPrice = vehicles[0].PricePerDay * 3,
                Status = ReservationStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Reservation
            {
                CustomerId = customer.Id,
                VehicleId = vehicles[2].Id,
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(9),
                TotalPrice = vehicles[2].PricePerDay * 2,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Reservations.AddRange(reservations);
        await context.SaveChangesAsync();
    }
}