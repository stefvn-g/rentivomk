using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentivoMK.DTOs;
using RentivoMK.Interfaces;

namespace RentivoMK.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/users — Admin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ResponseCache(Duration = 30)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // GET /api/users/{id} — Admin, Worker
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Worker")]
    [ResponseCache(Duration = 30)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(new { error = $"User with ID {id} not found." });

        return Ok(user);
    }

    // PUT /api/users/{id} — Admin only
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/users/{id} — Admin only
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
