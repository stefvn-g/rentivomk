using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Repositories;

public class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Reservation>> GetByCustomerIdAsync(int customerId)
        => await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .Where(r => r.CustomerId == customerId)
            .ToListAsync();

    public async Task<IEnumerable<Reservation>> GetAllWithDetailsAsync()
        => await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .ToListAsync();
}
