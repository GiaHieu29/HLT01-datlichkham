using Hlt.Api.Auth;
using Hlt.Application;
using Hlt.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hlt.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(AdminService admin) : ControllerBase
{
    [HttpGet("users")]
    public Task<List<UserDto>> Users(CancellationToken ct) => admin.UsersAsync(ct);

    [HttpPut("users/{id:int}/role")]
    public Task<UserDto> ChangeRole(int id, ChangeRoleRequest req, CancellationToken ct) => admin.ChangeRoleAsync(id, req, ct);

    [HttpPost("users/{id:int}/lock")]
    public Task<UserDto> Lock(int id, CancellationToken ct) => admin.SetLockedAsync(id, true, ct);

    [HttpPost("users/{id:int}/unlock")]
    public Task<UserDto> Unlock(int id, CancellationToken ct) => admin.SetLockedAsync(id, false, ct);

    [HttpPost("departments")]
    public Task<DepartmentDto> CreateDepartment(CreateDepartmentRequest req, CancellationToken ct) => admin.CreateDepartmentAsync(req, ct);

    [HttpPost("priority-levels")]
    public Task<PriorityDto> CreatePriority(SavePriorityRequest req, CancellationToken ct) => admin.CreatePriorityAsync(req, ct);

    [HttpPut("priority-levels/{id:int}")]
    public Task<PriorityDto> UpdatePriority(int id, SavePriorityRequest req, CancellationToken ct) => admin.UpdatePriorityAsync(id, req, ct);

    /// <summary>DELETE /api/admin/priority-levels/5 – xóa mức ưu tiên (409 nếu là mức mặc định hoặc đang được dùng).</summary>
    [HttpDelete("priority-levels/{id:int}")]
    public async Task<IActionResult> DeletePriority(int id, CancellationToken ct)
    {
        await admin.DeletePriorityAsync(id, ct);
        return NoContent();
    }

    /// <summary>GET /api/admin/schedules?doctorId=1&amp;from=2026-11-01&amp;to=2026-11-30</summary>
    [HttpGet("schedules")]
    public Task<List<ScheduleDto>> Schedules([FromQuery] int doctorId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => admin.SchedulesAsync(doctorId, from, to, ct);

    /// <summary>FR-18: tạo ca làm việc và tự sinh khung giờ.</summary>
    [HttpPost("schedules")]
    public Task<ScheduleDto> CreateSchedule(CreateScheduleRequest req, CancellationToken ct) => admin.CreateScheduleAsync(req, ct);
}