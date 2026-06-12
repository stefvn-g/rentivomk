using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync()
        => await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Available)
            .ToListAsync();
}