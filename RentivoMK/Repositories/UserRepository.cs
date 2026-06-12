using Microsoft.EntityFrameworkCore;
using RentivoMK.Data;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
}