using Hlt.Api.Auth;
using Hlt.Application;
using Hlt.Application.Services;
using Hlt.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hlt.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController(BookingService booking, StaffService staff) : ControllerBase
{
    // ----- Bệnh nhân -----

    /// <summary>UC-02: đặt lịch khám. 409 nếu khung giờ vừa bị người khác đặt.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Patient)]
    public async Task<ActionResult<AppointmentDto>> Book(BookRequest req, CancellationToken ct)
    {
        var dto = await booking.BookAsync(req, ct);
        return Created($"/api/appointments/{dto.Id}", dto);
    }

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Patient)]
    public Task<List<AppointmentDto>> Mine(CancellationToken ct) => booking.MineAsync(ct);

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{Roles.Patient},{Roles.Staff}")]
    public async Task<IActionResult> Cancel(int id, ReasonRequest req, CancellationToken ct)
    {
        if (User.IsInRole(Roles.Staff)) await staff.CancelAsync(id, req.Reason, ct);
        else await booking.CancelByPatientAsync(id, req.Reason, ct);
        return NoContent();
    }

    // ----- Nhân viên -----

    /// <summary>GET /api/staff/appointments?status=Pending&amp;departmentId=1&amp;date=2026-11-10</summary>
    [HttpGet("/api/staff/appointments")]
    [Authorize(Roles = Roles.Staff)]
    public Task<List<AppointmentDto>> StaffList([FromQuery] AppointmentStatus? status, [FromQuery] int? departmentId,
        [FromQuery] DateOnly? date, CancellationToken ct)
        => staff.ListAsync(status, departmentId, date, ct);

    /// <summary>Tìm lịch hẹn HÔM NAY theo mã hoặc tên bệnh nhân (để check-in).</summary>
    [HttpGet("/api/staff/appointments/search")]
    [Authorize(Roles = Roles.Staff)]
    public Task<List<AppointmentDto>> Search([FromQuery] string? keyword, [FromQuery] int? departmentId, CancellationToken ct)
        => staff.SearchForCheckInAsync(keyword ?? "", departmentId, ct);

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = Roles.Staff)]
    public Task<AppointmentDto> Confirm(int id, ConfirmRequest req, CancellationToken ct) => staff.ConfirmAsync(id, req.PriorityId, ct);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Staff)]
    public Task<AppointmentDto> Reject(int id, ReasonRequest req, CancellationToken ct) => staff.RejectAsync(id, req.Reason, ct);

    [HttpPost("{id:int}/reschedule")]
    [Authorize(Roles = Roles.Staff)]
    public Task<AppointmentDto> Reschedule(int id, RescheduleRequest req, CancellationToken ct)
        => staff.RescheduleAsync(id, req.NewSlotId, req.Reason, ct);

    [HttpPost("{id:int}/check-in")]
    [Authorize(Roles = Roles.Staff)]
    public Task<TicketDto> CheckIn(int id, CancellationToken ct) => staff.CheckInAsync(id, ct);

    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = Roles.Staff)]
    public Task<AppointmentDto> NoShow(int id, ReasonRequest req, CancellationToken ct) => staff.MarkNoShowAsync(id, req.Reason, ct);
}