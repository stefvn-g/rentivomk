using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentivoMK.DTOs;
using RentivoMK.Interfaces;

namespace RentivoMK.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // GET /api/vehicles — All authenticated
    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }

    // GET /api/vehicles/available — All authenticated
    [HttpGet("available")]
    [ResponseCache(Duration = 30)]
    public async Task<IActionResult> GetAvailable()
    {
        var vehicles = await _vehicleService.GetAvailableVehiclesAsync();
        return Ok(vehicles);
    }

    // GET /api/vehicles/{id} — All authenticated
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle is null)
            return NotFound(new { error = $"Vehicle with ID {id} not found." });

        return Ok(vehicle);
    }

    // POST /api/vehicles — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        var created = await _vehicleService.CreateVehicleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/vehicles/{id} — Admin only
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        await _vehicleService.UpdateVehicleAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/vehicles/{id} — Admin only
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _vehicleService.DeleteVehicleAsync(id);
        return NoContent();
    }
}
