namespace Hlt.Domain.Appointments;

/// <summary>Một bản ghi lịch sử thay đổi của lịch hẹn (FR-11, NFR-05).</summary>
public class AppointmentHistory
{
    public int Id { get; private set; }
    public int AppointmentId { get; private set; }
    public Appointment Appointment { get; private set; } = null!;
    public int ChangedBy { get; private set; }
    public ChangeType ChangeType { get; private set; }
    public AppointmentStatus? OldStatus { get; private set; }
    public AppointmentStatus? NewStatus { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }

    private AppointmentHistory() { }

    internal AppointmentHistory(Appointment appointment, int changedBy, ChangeType type,
        AppointmentStatus? oldStatus, AppointmentStatus? newStatus, string? reason)
    {
        Appointment = appointment;
        ChangedBy = changedBy;
        ChangeType = type;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason is { Length: > 500 } ? reason[..500] : reason;
        ChangedAt = DateTimeOffset.UtcNow;
    }
}