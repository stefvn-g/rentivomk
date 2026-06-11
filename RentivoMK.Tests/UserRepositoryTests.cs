using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Enums;
using RentivoMK.Models;
using RentivoMK.Repositories;

namespace RentivoMK.Tests;

public class UserRepositoryTests
{
    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_ReturnsAllUsers));
        context.Users.AddRange(
            new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "hash1", Role = UserRole.Customer },
            new User { FirstName = "Admin", LastName = "User", Email = "admin@test.com", PasswordHash = "hash2", Role = UserRole.Admin }
        );
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsUser_WhenExists));
        var user = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "hash", Role = UserRole.Customer };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("jane@test.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenEmailExists()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByEmailAsync_ReturnsUser_WhenEmailExists));
        var user = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "hash", Role = UserRole.Customer };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByEmailAsync("jane@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jane@test.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenEmailNotFound()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByEmailAsync_ReturnsNull_WhenEmailNotFound));
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByEmailAsync("nobody@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsUserToDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(AddAsync_PersistsUserToDatabase));
        var repo = new UserRepository(context);

        var user = new User { FirstName = "John", LastName = "Smith", Email = "john@test.com", PasswordHash = "hash", Role = UserRole.Customer };

        // Act
        await repo.AddAsync(user);

        // Assert
        var saved = await context.Users.FirstOrDefaultAsync(u => u.Email == "john@test.com");
        Assert.NotNull(saved);
        Assert.Equal("John", saved.FirstName);
        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        // Arrange
        using var context = CreateContext(nameof(UpdateAsync_PersistsChanges));
        var user = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "hash", Role = UserRole.Customer };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        user.FirstName = "Janet";
        user.Role = UserRole.Worker;
        await repo.UpdateAsync(user);

        // Assert
        var updated = await context.Users.FindAsync(user.Id);
        Assert.Equal("Janet", updated!.FirstName);
        Assert.Equal(UserRole.Worker, updated.Role);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserFromDatabase()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_RemovesUserFromDatabase));
        var user = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "hash", Role = UserRole.Customer };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        int userId = user.Id;

        // Act
        await repo.DeleteAsync(user);

        // Assert
        var deleted = await context.Users.FindAsync(userId);
        Assert.Null(deleted);
    }
}