using Hlt.Api.Auth;
using Hlt.Application;
using Hlt.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hlt.Api.Controllers;

[ApiController]
[Authorize]
public class QueuesController(QueueService queue) : ControllerBase
{
    /// <summary>GET /api/queues?departmentId=1 – hàng đợi của khoa, đã sắp theo thứ tự gọi.</summary>
    [HttpGet("api/queues")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Doctor}")]
    public Task<List<TicketDto>> Get([FromQuery] int departmentId, [FromQuery] DateOnly? date, CancellationToken ct)
        => queue.QueueAsync(departmentId, date, ct);

    /// <summary>UC-13: gọi bệnh nhân tiếp theo. 204 nếu không còn ai chờ.</summary>
    [HttpPost("api/queues/call-next")]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<ActionResult<TicketDto>> CallNext(CancellationToken ct)
    {
        var ticket = await queue.CallNextAsync(ct);
        return ticket is null ? NoContent() : Ok(ticket);
    }

    /// <summary>Các số bác sĩ đang gọi / đang khám.</summary>
    [HttpGet("api/queues/current")]
    [Authorize(Roles = Roles.Doctor)]
    public Task<List<TicketDto>> Current(CancellationToken ct) => queue.CurrentForDoctorAsync(ct);

    /// <summary>MH-07: màn hình số thứ tự công khai – không cần đăng nhập, không có thông tin cá nhân.</summary>
    [HttpGet("api/queues/display")]
    [AllowAnonymous]
    public Task<DisplayDto> Display([FromQuery] int departmentId, CancellationToken ct) => queue.DisplayAsync(departmentId, ct);

    [HttpPost("api/queue-tickets/{id:int}/complete")]
    [Authorize(Roles = Roles.Doctor)]
    public Task<TicketDto> Complete(int id, CancellationToken ct) => queue.CompleteAsync(id, ct);

    [HttpPost("api/queue-tickets/{id:int}/skip")]
    [Authorize(Roles = Roles.Doctor)]
    public Task<TicketDto> Skip(int id, CancellationToken ct) => queue.SkipAsync(id, ct);

    /// <summary>UC-12: lịch làm việc trong ngày của bác sĩ.</summary>
    [HttpGet("api/doctor/appointments")]
    [Authorize(Roles = Roles.Doctor)]
    public Task<List<AppointmentDto>> DoctorDay([FromQuery] DateOnly? date, CancellationToken ct) => queue.DoctorDayAsync(date, ct);
    
    /// <summary>GET /api/doctor/schedule?from=2026-11-10&amp;to=2026-12-10 – mọi lịch hẹn của bác sĩ trong khoảng ngày.</summary>
    [HttpGet("api/doctor/schedule")]
    [Authorize(Roles = Roles.Doctor)]
    public Task<List<AppointmentDto>> DoctorSchedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => queue.DoctorScheduleAsync(from, to, ct);
}