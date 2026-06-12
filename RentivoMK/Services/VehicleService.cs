using Microsoft.Extensions.Caching.Memory;
using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IMemoryCache _cache;
    private const string KeyAll = "vehicles:all:v1";
    private const string KeyAvailable = "vehicles:available:v1";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
        SlidingExpiration = TimeSpan.FromSeconds(15)
    };
    public VehicleService(IVehicleRepository vehicleRepository, IReservationRepository reservationRepository, IMemoryCache cache)
    {
        _vehicleRepository = vehicleRepository;
        _reservationRepository = reservationRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync()
    {
        if (_cache.TryGetValue<IEnumerable<VehicleDto>>(KeyAll, out var cached))
            return cached!;

        var vehicles = await _vehicleRepository.GetAllAsync();
        var dto = vehicles.Select(MapToDto).ToList();
        _cache.Set(KeyAll, dto, CacheOptions);
        return dto;
    }

    public async Task<IEnumerable<VehicleDto>> GetAvailableVehiclesAsync()
    {
        if (_cache.TryGetValue<IEnumerable<VehicleDto>>(KeyAvailable, out var cached))
            return cached!;

        var vehicles = await _vehicleRepository.GetAvailableVehiclesAsync();
        var dto = vehicles.Select(MapToDto).ToList();
        _cache.Set(KeyAvailable, dto, CacheOptions);
        return dto;
    }

    public async Task<VehicleDto?> GetVehicleByIdAsync(int id)
    {
        var key = $"vehicles:id:{id}:v1";

        if (_cache.TryGetValue<VehicleDto>(key, out var cached))
            return cached!;

        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle is null) return null;

        var dto = MapToDto(vehicle);
        _cache.Set(key, dto, CacheOptions);
        return dto;
    }

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        var vehicle = new Vehicle
        {
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            PricePerDay = dto.PricePerDay,
            Category = dto.Category,
            Description = dto.Description,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _vehicleRepository.AddAsync(vehicle);

        InvalidateVehicleCaches();

        return MapToDto(vehicle);
    }

    public async Task UpdateVehicleAsync(int id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

        vehicle.Make = dto.Make;
        vehicle.Model = dto.Model;
        vehicle.Year = dto.Year;
        vehicle.PricePerDay = dto.PricePerDay;
        vehicle.Category = dto.Category;
        vehicle.Description = dto.Description;

        if (dto.Status.HasValue)
            vehicle.Status = dto.Status.Value;

        await _vehicleRepository.UpdateAsync(vehicle);
        InvalidateVehicleCaches();
        _cache.Remove($"vehicles:id:{id}:v1");
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

        var reservations = await _reservationRepository.GetAllWithDetailsAsync();
        var hasActive = reservations.Any(r =>
            r.VehicleId == id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved));

        if (hasActive)
            throw new InvalidOperationException("Cannot delete a vehicle with active (Pending or Approved) reservations.");

        await _vehicleRepository.DeleteAsync(vehicle);
        InvalidateVehicleCaches();
        _cache.Remove($"vehicles:id:{id}:v1");
    }
    private void InvalidateVehicleCaches()
    {
        _cache.Remove(KeyAll);
        _cache.Remove(KeyAvailable);
    }

    private static VehicleDto MapToDto(Vehicle vehicle) => new VehicleDto
    {
        Id = vehicle.Id,
        Make = vehicle.Make,
        Model = vehicle.Model,
        Year = vehicle.Year,
        PricePerDay = vehicle.PricePerDay,
        Category = vehicle.Category,
        Status = vehicle.Status,
        Description = vehicle.Description,
        CreatedAt = vehicle.CreatedAt
    };
}