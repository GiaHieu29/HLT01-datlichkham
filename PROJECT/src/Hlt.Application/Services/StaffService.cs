using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Common;
using Hlt.Domain.Queueing;

namespace Hlt.Application.Services;

/// <summary>Use case của nhân viên: xác nhận (UC-07, UC-08), đổi lịch (UC-09), hủy, check-in (UC-10), vắng mặt.</summary>
public class StaffService(
    IDirectoryRepository directory,
    IScheduleRepository schedules,
    IAppointmentRepository appointments,
    IQueueRepository queue,
    AppointmentEvents events,
    QueueManager queueManager,
    IUnitOfWork uow,
    ICurrentUser me,
    IClock clock,
    BookingRules rules)
{
    public async Task<List<AppointmentDto>> ListAsync(AppointmentStatus? status, int? departmentId, DateOnly? date, CancellationToken ct)
    {
        var list = await appointments.ListForStaffAsync(status, departmentId, date, ct);
        return list.Select(a => a.ToDto(false)).ToList();
    }

    public async Task<List<AppointmentDto>> SearchForCheckInAsync(string keyword, int? departmentId, CancellationToken ct)
    {
        var list = await appointments.SearchAsync(keyword ?? "", clock.Today, departmentId, ct);
        return list.Select(a => a.ToDto(false)).ToList();
    }

    public async Task<AppointmentDto> ConfirmAsync(int id, int priorityId, CancellationToken ct)
    {
        var staff = await directory.StaffByUserAsync(me.UserId, ct) ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ nhân viên.");
        var priority = await directory.PriorityAsync(priorityId, ct) ?? throw DomainException.NotFound("Mức ưu tiên không tồn tại.");
        var a = await Load(id, ct);

        a.Confirm(staff, priority, me.UserId);
        events.Publish(a, ChangeType.Confirm, Messages.Confirmed(a, clock.ClinicOffset));
        await uow.SaveChangesAsync(ct);
        return a.ToDto(false);
    }

    public async Task<AppointmentDto> RejectAsync(int id, string? reason, CancellationToken ct)
    {
        var a = await Load(id, ct);
        a.Reject(reason ?? "", me.UserId);
        events.Publish(a, ChangeType.Reject, Messages.Rejected(a, clock.ClinicOffset, reason ?? ""));
        await uow.SaveChangesAsync(ct);
        return a.ToDto(false);
    }

    public async Task<AppointmentDto> CancelAsync(int id, string? reason, CancellationToken ct)
    {
        var a = await Load(id, ct);
        a.Cancel(reason, me.UserId);
        events.Publish(a, ChangeType.Cancel, Messages.Cancelled(a, clock.ClinicOffset, reason));
        await uow.SaveChangesAsync(ct);
        return a.ToDto(false);
    }

    /// <summary>UC-09: giữ giờ mới, nhả giờ cũ, ghi lịch sử, thông báo – tất cả trong MỘT lần SaveChanges (một giao dịch).</summary>
    public async Task<AppointmentDto> RescheduleAsync(int id, int newSlotId, string? reason, CancellationToken ct)
    {
        var a = await Load(id, ct);
        var newSlot = await schedules.GetSlotAsync(newSlotId, ct) ?? throw DomainException.NotFound("Không tìm thấy khung giờ mới.");
        if (newSlot.StartAt <= clock.Now) throw new DomainException("Không thể đổi sang khung giờ đã qua.");

        a.Reschedule(newSlot, reason, me.UserId);
        events.Publish(a, ChangeType.Reschedule, Messages.Rescheduled(a, clock.ClinicOffset));
        await uow.SaveChangesAsync(ct); // luồng 3a: khung giờ mới vừa bị đặt -> 409, lịch cũ giữ nguyên
        return a.ToDto(false);
    }

    /// <summary>UC-10: check-in và cấp số thứ tự theo khoa, ngày (BR-06).</summary>
    public async Task<TicketDto> CheckInAsync(int id, CancellationToken ct)
    {
        var a = await Load(id, ct);
        var today = clock.Today;
        var slotDate = DateOnly.FromDateTime(a.Slot.StartAt.ToOffset(clock.ClinicOffset).DateTime);
        if (slotDate != today) throw new DomainException("Chỉ check-in vào đúng ngày khám.");
        if (clock.Now > a.Slot.StartAt + rules.MaxLate)
            throw new DomainException($"Bệnh nhân đến muộn quá {rules.MaxLateMinutes} phút. Hãy đổi lịch hoặc đánh dấu vắng mặt.");

        // Cấp số + lưu trong cùng giao dịch, có khóa để hai quầy check-in không cấp trùng số.
        return await uow.InTransactionAsync(async () =>
        {
            var no = await queue.NextTicketNoAsync(a.Doctor.DepartmentId, today, ct);
            var ticket = queueManager.CheckIn(a, a.Doctor.DepartmentId, today, no, clock.Now, me.UserId);
            queue.Add(ticket);
            events.Publish(a, ChangeType.CheckIn, Messages.CheckedIn(a, no));
            await uow.SaveChangesAsync(ct);
            return ticket.ToDto();
        }, ct);
    }

    public async Task<AppointmentDto> MarkNoShowAsync(int id, string? reason, CancellationToken ct)
    {
        var a = await Load(id, ct);
        a.MarkNoShow(reason, me.UserId);
        var ticket = await queue.GetByAppointmentAsync(id, ct);
        if (ticket is { Status: TicketStatus.Waiting or TicketStatus.Called }) ticket.Skip();
        events.Publish(a, ChangeType.NoShow, Messages.NoShow(a, clock.ClinicOffset));
        await uow.SaveChangesAsync(ct);
        return a.ToDto(false);
    }

    private async Task<Hlt.Domain.Appointments.Appointment> Load(int id, CancellationToken ct)
        => await appointments.GetAsync(id, ct) ?? throw DomainException.NotFound("Không tìm thấy lịch hẹn.");
}