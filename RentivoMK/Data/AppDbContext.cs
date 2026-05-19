using Microsoft.EntityFrameworkCore;
using RentivoMK.Models;

namespace RentivoMK.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(v => v.Status).HasConversion<string>();
            entity.Property(v => v.PricePerDay).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>();
            entity.Property(r => r.TotalPrice).HasColumnType("decimal(10,2)");

            // Deleting a User cascades to their Reservations
            entity.HasOne(r => r.Customer)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a Vehicle is restricted if reservations exist
            entity.HasOne(r => r.Vehicle)
                .WithMany(v => v.Reservations)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}