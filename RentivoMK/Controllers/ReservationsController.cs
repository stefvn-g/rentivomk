using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentivoMK.DTOs;
using RentivoMK.Interfaces;

namespace RentivoMK.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity could not be determined.");
        return int.Parse(claim);
    }

    private bool IsAdminOrWorker()
        => User.IsInRole("Admin") || User.IsInRole("Worker");

    // GET /api/reservations — Admin, Worker
    [HttpGet]
    [Authorize(Roles = "Admin,Worker")]
    [ResponseCache(Duration = 15)]
    public async Task<IActionResult> GetAll()
    {
        var reservations = await _reservationService.GetAllReservationsAsync();
        return Ok(reservations);
    }

    // GET /api/reservations/my — Customer only
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    [ResponseCache(Duration = 15)]
    public async Task<IActionResult> GetMy()
    {
        var userId = GetCurrentUserId();
        var reservations = await _reservationService.GetMyReservationsAsync(userId);
        return Ok(reservations);
    }

    // GET /api/reservations/{id} — Admin, Worker, or owning Customer
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 15)]
    public async Task<IActionResult> GetById(int id)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(id);
        if (reservation is null)
            return NotFound(new { error = $"Reservation with ID {id} not found." });

        if (!IsAdminOrWorker())
        {
            var userId = GetCurrentUserId();
            if (reservation.CustomerId != userId)
                return Forbid();
        }

        return Ok(reservation);
    }

    // POST /api/reservations — Customer only
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var userId = GetCurrentUserId();
        var created = await _reservationService.CreateReservationAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/reservations/{id}/approve — Worker, Admin
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Worker")]
    public async Task<IActionResult> Approve(int id)
    {
        await _reservationService.ApproveReservationAsync(id);
        return NoContent();
    }

    // PUT /api/reservations/{id}/reject — Worker, Admin
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Worker")]
    public async Task<IActionResult> Reject(int id)
    {
        await _reservationService.RejectReservationAsync(id);
        return NoContent();
    }

    // PUT /api/reservations/{id}/complete — Worker, Admin
    [HttpPut("{id:int}/complete")]
    [Authorize(Roles = "Admin,Worker")]
    public async Task<IActionResult> Complete(int id)
    {
        await _reservationService.CompleteReservationAsync(id);
        return NoContent();
    }

    // PUT /api/reservations/{id}/cancel — Customer, Admin
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        await _reservationService.CancelReservationAsync(id, userId, isAdmin);
        return NoContent();
    }
}
