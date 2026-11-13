using Hlt.Api.Auth;
using Hlt.Application;
using Hlt.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hlt.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class CatalogController(CatalogService catalog, AccountService account) : ControllerBase
{
    /// <summary>Thông tin người đang đăng nhập (giao diện dùng để biết vai trò).</summary>
    [HttpGet("me")]
    public Task<MeDto> Me(CancellationToken ct) => account.MeAsync(ct);

    [HttpGet("notifications")]
    public Task<List<NotificationDto>> Notifications(CancellationToken ct) => account.NotificationsAsync(ct);

    [HttpGet("departments")]
    [AllowAnonymous] // màn hình số thứ tự công khai cũng cần chọn khoa
    public Task<List<DepartmentDto>> Departments(CancellationToken ct) => catalog.DepartmentsAsync(ct);

    [HttpGet("doctors")]
    public Task<List<DoctorDto>> Doctors([FromQuery] int? departmentId, CancellationToken ct)
        => catalog.DoctorsAsync(departmentId, ct);

    [HttpGet("priority-levels")]
    public Task<List<PriorityDto>> Priorities(CancellationToken ct) => catalog.PrioritiesAsync(ct);

    /// <summary>GET /api/slots?doctorId=1&amp;date=2026-11-10 – khung giờ của bác sĩ trong ngày.</summary>
    [HttpGet("slots")]
    [Authorize(Roles = $"{Roles.Patient},{Roles.Staff}")]
    public Task<List<SlotDto>> Slots([FromQuery] int doctorId, [FromQuery] DateOnly date, CancellationToken ct)
        => catalog.SlotsAsync(doctorId, date, ct);

    /// <summary>Giữ chỗ tạm khi bệnh nhân bấm «Chọn».</summary>
    [HttpPost("slots/{id:int}/hold")]
    [Authorize(Roles = Roles.Patient)]
    public Task<SlotDto> Hold(int id, CancellationToken ct) => catalog.HoldAsync(id, ct);
}