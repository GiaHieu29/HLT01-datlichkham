using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Queueing;

namespace Hlt.Application;

// ---------- Dữ liệu trả về cho giao diện (DTO) ----------

public record MeDto(int Id, string FullName, string Email, string Role, int? DepartmentId);

public record DepartmentDto(int Id, string Name, string? Location);

public record DoctorDto(int Id, string FullName, int DepartmentId, string DepartmentName, string? Specialty);

public record PriorityDto(int Id, string Code, string Name, short LevelOrder, string? Description);

/// <summary>Trạng thái hiển thị: AVAILABLE, BOOKED, BLOCKED, HELD (người khác giữ), MINE (mình đang giữ), PAST.</summary>
public record SlotDto(int Id, DateTimeOffset StartAt, DateTimeOffset EndAt, string Status);

public record AppointmentDto(
    int Id,
    string PatientName,
    string DoctorName,
    int DepartmentId,
    string DepartmentName,
    int SlotId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    string StatusText,
    int PriorityId,
    string PriorityName,
    string? Reason,
    DateTimeOffset CreatedAt,
    bool CanCancel,
    int? TicketNo = null,
    string? TicketStatus = null,
    int? PeopleAhead = null);

public record TicketDto(
    int Id,
    int TicketNo,
    int AppointmentId,
    string PatientName,
    string DoctorName,
    string PriorityName,
    short PriorityOrder,
    string Status,
    string StatusText,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? CalledAt,
    string? Reason);

public record DisplayDto(string DepartmentName, List<DisplayCallDto> Calling, List<int> NextNumbers, int WaitingCount);
public record DisplayCallDto(int TicketNo, string DoctorName);

public record NotificationDto(int Id, string Channel, string Content, string Status, DateTimeOffset? SentAt, int? AppointmentId);

public record UserDto(int Id, string FullName, string Email, string Role, string Status);

public record ScheduleDto(int Id, int DoctorId, DateOnly WorkDate, TimeOnly StartTime, TimeOnly EndTime, short SlotMinutes, int SlotCount);

// ---------- Dữ liệu gửi lên từ giao diện ----------

public record BookRequest(int SlotId, string? Reason);
public record ConfirmRequest(int PriorityId);
public record ReasonRequest(string? Reason);
public record RescheduleRequest(int NewSlotId, string? Reason);
public record CreateDepartmentRequest(string Name, string? Location);
public record SavePriorityRequest(string Code, string Name, short LevelOrder, string? Description);
public record CreateScheduleRequest(int DoctorId, DateOnly WorkDate, TimeOnly StartTime, TimeOnly EndTime, short SlotMinutes, short? MaxPatients);
public record ChangeRoleRequest(string Role, int? DepartmentId);

// ---------- Chuyển entity -> DTO ----------

public static class Mapping
{
    public static AppointmentDto ToDto(this Appointment a, bool canCancel, QueueTicket? ticket = null, int? peopleAhead = null)
        => new(
            a.Id,
            a.Patient.FullName,
            a.Doctor.Title,
            a.Doctor.DepartmentId,
            a.Doctor.Department?.Name ?? "",
            a.SlotId,
            a.Slot.StartAt,
            a.Slot.EndAt,
            a.Status.ToString(),
            StatusText.Vi(a.Status),
            a.PriorityId,
            a.Priority.Name,
            a.Reason,
            a.CreatedAt,
            canCancel,
            ticket?.TicketNo,
            ticket?.Status.ToString(),
            peopleAhead);

    public static TicketDto ToDto(this QueueTicket t)
        => new(
            t.Id,
            t.TicketNo,
            t.AppointmentId,
            t.Appointment.Patient.FullName,
            t.Appointment.Doctor.Title,
            t.Priority.Name,
            t.Priority.LevelOrder,
            t.Status.ToString(),
            StatusText.Vi(t.Status),
            t.EnqueuedAt,
            t.CalledAt,
            t.Appointment.Reason);
}