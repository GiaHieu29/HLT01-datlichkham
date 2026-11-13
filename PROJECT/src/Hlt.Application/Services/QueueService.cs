using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Common;
using Hlt.Domain.Queueing;

namespace Hlt.Application.Services;

/// <summary>Điều phối hàng đợi: xem hàng đợi (UC-11), gọi số (UC-13), hoàn tất (UC-14), màn hình số thứ tự.</summary>
public class QueueService(
    IDirectoryRepository directory,
    IAppointmentRepository appointments,
    IQueueRepository queue,
    AppointmentEvents events,
    QueueManager queueManager,
    IUnitOfWork uow,
    ICurrentUser me,
    IClock clock)
{
    /// <summary>Hàng đợi của khoa, sắp theo đúng thứ tự sẽ được gọi.</summary>
    public async Task<List<TicketDto>> QueueAsync(int departmentId, DateOnly? date, CancellationToken ct)
    {
        var all = await queue.ListAsync(departmentId, date ?? clock.Today, ct);
        var waiting = queueManager.Strategy.Order(all.Where(t => t.Status == TicketStatus.Waiting));
        var others = all.Where(t => t.Status != TicketStatus.Waiting).OrderByDescending(t => t.CalledAt ?? t.EnqueuedAt);
        var called = others.Where(t => t.Status == TicketStatus.Called);
        var finished = others.Where(t => t.Status != TicketStatus.Called);
        return called.Concat(waiting).Concat(finished).Select(t => t.ToDto()).ToList();
    }

    /// <summary>UC-13: gọi bệnh nhân tiếp theo của bác sĩ đang đăng nhập.</summary>
    public async Task<TicketDto?> CallNextAsync(CancellationToken ct)
    {
        var doctor = await directory.DoctorByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bác sĩ.");
        var today = clock.Today;

        return await uow.InTransactionAsync(async () =>
        {
            // Khóa các số đang chờ (UPDLOCK) để hai lần bấm gọi cùng lúc không chọn trùng một người.
            var waiting = await queue.LockWaitingAsync(doctor.DepartmentId, today, doctor.Id, ct);
            var next = queueManager.CallNext(waiting, clock.Now, me.UserId); // Strategy chọn người
            if (next is null) return null;

            events.Publish(next.Appointment, ChangeType.StartExam, Messages.Called(next.Appointment, next.TicketNo));
            await uow.SaveChangesAsync(ct);
            return next.ToDto();
        }, ct);
    }

    public async Task<List<TicketDto>> CurrentForDoctorAsync(CancellationToken ct)
    {
        var doctor = await directory.DoctorByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bác sĩ.");
        return (await queue.CalledForDoctorAsync(doctor.Id, clock.Today, ct)).Select(t => t.ToDto()).ToList();
    }

    public async Task<TicketDto> CompleteAsync(int ticketId, CancellationToken ct)
    {
        var t = await LoadForDoctor(ticketId, ct);
        queueManager.Complete(t, me.UserId);
        events.Publish(t.Appointment, ChangeType.Complete, Messages.Completed(t.Appointment));
        await uow.SaveChangesAsync(ct);
        return t.ToDto();
    }

    public async Task<TicketDto> SkipAsync(int ticketId, CancellationToken ct)
    {
        var t = await LoadForDoctor(ticketId, ct);
        queueManager.Skip(t, me.UserId);
        events.Publish(t.Appointment, ChangeType.NoShow, Messages.NoShow(t.Appointment, clock.ClinicOffset));
        await uow.SaveChangesAsync(ct);
        return t.ToDto();
    }

    /// <summary>UC-12: lịch làm việc trong ngày của bác sĩ.</summary>
    public async Task<List<AppointmentDto>> DoctorDayAsync(DateOnly? date, CancellationToken ct)
    {
        var doctor = await directory.DoctorByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bác sĩ.");
        return (await appointments.ListForDoctorAsync(doctor.Id, date ?? clock.Today, ct)).Select(a => a.ToDto(false)).ToList();
    }
    /// <summary>UC-12 mở rộng: mọi lịch hẹn của bác sĩ trong khoảng ngày (mặc định hôm nay đến 30 ngày tới).</summary>
    public async Task<List<AppointmentDto>> DoctorScheduleAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var doctor = await directory.DoctorByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bác sĩ.");
        var start = from ?? clock.Today;
        var end = to ?? start.AddDays(30);
        if (end < start) throw new DomainException("Ngày kết thúc phải sau ngày bắt đầu.");
        if (end.DayNumber - start.DayNumber > 92) throw new DomainException("Chỉ xem tối đa 3 tháng mỗi lần.");
        return (await appointments.ListForDoctorRangeAsync(doctor.Id, start, end, ct)).Select(a => a.ToDto(false)).ToList();
    }
    /// <summary>FR-15: dữ liệu màn hình số thứ tự công khai – KHÔNG chứa thông tin cá nhân.</summary>
    public async Task<DisplayDto> DisplayAsync(int departmentId, CancellationToken ct)
    {
        var dept = await directory.DepartmentAsync(departmentId, ct) ?? throw DomainException.NotFound("Không tìm thấy khoa.");
        var all = await queue.ListAsync(departmentId, clock.Today, ct);
        var calling = all.Where(t => t.Status == TicketStatus.Called)
                         .OrderByDescending(t => t.CalledAt)
                         .Select(t => new DisplayCallDto(t.TicketNo, t.Appointment.Doctor.Title))
                         .ToList();
        var waiting = queueManager.Strategy.Order(all.Where(t => t.Status == TicketStatus.Waiting)).ToList();
        return new DisplayDto(dept.Name, calling, waiting.Take(5).Select(t => t.TicketNo).ToList(), waiting.Count);
    }

    private async Task<QueueTicket> LoadForDoctor(int ticketId, CancellationToken ct)
    {
        var doctor = await directory.DoctorByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bác sĩ.");
        var t = await queue.GetAsync(ticketId, ct) ?? throw DomainException.NotFound("Không tìm thấy số thứ tự.");
        if (t.Appointment.DoctorId != doctor.Id) throw DomainException.Forbidden("Số thứ tự này không thuộc bác sĩ hiện tại.");
        return t;
    }
}