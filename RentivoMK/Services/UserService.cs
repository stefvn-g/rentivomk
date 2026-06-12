using Microsoft.Extensions.Caching.Memory;
using RentivoMK.DTOs;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;
    private const string KeyAll = "users:all:v1";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
        SlidingExpiration = TimeSpan.FromSeconds(15)
    };
    public UserService(IUserRepository userRepository, IMemoryCache cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        if (_cache.TryGetValue<IEnumerable<UserDto>>(KeyAll, out var cached))
            return cached!;

        var users = await _userRepository.GetAllAsync();
        var dto = users.Select(MapToDto).ToList();
        _cache.Set(KeyAll, dto, CacheOptions);
        return dto;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;

        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;

        await _userRepository.UpdateAsync(user);
        _cache.Remove(KeyAll);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        await _userRepository.DeleteAsync(user);
        _cache.Remove(KeyAll);
    }

    private static UserDto MapToDto(User user) => new UserDto
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}