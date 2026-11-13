using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Common;
using Hlt.Domain.Queueing;

namespace Hlt.Application.Services;

/// <summary>Các use case của bệnh nhân: đặt lịch (UC-02), hủy lịch (UC-03), xem lịch & số thứ tự (UC-05).</summary>
public class BookingService(
    IDirectoryRepository directory,
    IScheduleRepository schedules,
    IAppointmentRepository appointments,
    IQueueRepository queue,
    ISlotHoldStore holds,
    AppointmentEvents events,
    QueueManager queueManager,
    IUnitOfWork uow,
    ICurrentUser me,
    IClock clock,
    BookingRules rules)
{
    public async Task<AppointmentDto> BookAsync(BookRequest req, CancellationToken ct)
    {
        var patient = await directory.PatientByUserAsync(me.UserId, ct)
                      ?? throw DomainException.Forbidden("Chỉ tài khoản bệnh nhân mới được đặt lịch.");

        // Lớp bảo vệ phụ: giữ chỗ tạm (luồng thay thế 4b)
        var holder = holds.HolderOf(req.SlotId);
        if (holder is not null && holder != me.UserId)
            throw DomainException.Conflict("Khung giờ đang được người khác giữ chỗ. Vui lòng chọn giờ khác.");

        var slot = await schedules.GetSlotAsync(req.SlotId, ct) ?? throw DomainException.NotFound("Không tìm thấy khung giờ.");
        if (slot.StartAt <= clock.Now) throw new DomainException("Không thể đặt khung giờ đã qua.");

        var priority = await directory.DefaultPriorityAsync(ct);
        var appointment = Appointment.Book(patient, slot, priority, req.Reason, me.UserId); // slot.Book() bên trong
        appointments.Add(appointment);
        events.Publish(appointment, ChangeType.Create, Messages.Booked(appointment, clock.ClinicOffset));

        // Lớp bảo vệ chính nằm ở SQL Server: rowversion của time_slot + chỉ mục duy nhất có điều kiện.
        // Nếu hai người cùng đặt, người đến sau nhận DomainException(Conflict) -> HTTP 409.
        await uow.SaveChangesAsync(ct);
        holds.Release(req.SlotId);

        return appointment.ToDto(appointment.CanPatientCancel(clock.Now, rules.CancelDeadline));
    }

    public async Task<List<AppointmentDto>> MineAsync(CancellationToken ct)
    {
        var patient = await directory.PatientByUserAsync(me.UserId, ct)
                      ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bệnh nhân.");
        var list = await appointments.ListForPatientAsync(patient.Id, ct);
        var result = new List<AppointmentDto>();

        foreach (var a in list)
        {
            QueueTicket? ticket = null;
            int? ahead = null;
            if (a.Status is AppointmentStatus.CheckedIn or AppointmentStatus.InProgress or AppointmentStatus.Completed)
            {
                ticket = await queue.GetByAppointmentAsync(a.Id, ct);
                if (ticket is { Status: TicketStatus.Waiting })
                {
                    var all = await queue.ListAsync(ticket.DepartmentId, ticket.QueueDate, ct);
                    ahead = queueManager.PeopleAhead(all, all.First(t => t.Id == ticket.Id));
                }
            }
            result.Add(a.ToDto(a.CanPatientCancel(clock.Now, rules.CancelDeadline), ticket, ahead));
        }
        return result;
    }

    public async Task CancelByPatientAsync(int appointmentId, string? reason, CancellationToken ct)
    {
        var patient = await directory.PatientByUserAsync(me.UserId, ct)
                      ?? throw DomainException.Forbidden("Không tìm thấy hồ sơ bệnh nhân.");
        var a = await appointments.GetAsync(appointmentId, ct) ?? throw DomainException.NotFound("Không tìm thấy lịch hẹn.");
        if (a.PatientId != patient.Id) throw DomainException.Forbidden("Bạn không thể hủy lịch hẹn của người khác.");

        a.EnsureCancellableByPatient(clock.Now, rules.CancelDeadline);
        a.Cancel(reason ?? "Bệnh nhân tự hủy", me.UserId);
        events.Publish(a, ChangeType.Cancel, Messages.Cancelled(a, clock.ClinicOffset, reason));
        await uow.SaveChangesAsync(ct);
    }
}