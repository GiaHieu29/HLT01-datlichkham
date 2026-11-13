using Hlt.Domain.Common;
using Hlt.Domain.Organization;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;

namespace Hlt.Domain.Appointments;

/// <summary>
/// Lịch hẹn – lớp trung tâm của miền. Vòng đời được quản lý bằng mẫu State:
/// mỗi trạng thái (AppointmentState) quyết định thao tác nào hợp lệ.
/// Mọi thay đổi đều ghi vào lịch sử (AppointmentHistory).
/// </summary>
public class Appointment
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public int DoctorId { get; private set; }
    public Doctor Doctor { get; private set; } = null!;
    public int SlotId { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public int PriorityId { get; private set; }
    public PriorityLevel Priority { get; private set; } = null!;
    public int? ConfirmedBy { get; private set; }
    public Staff? ConfirmedByStaff { get; private set; }
    public string? Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cột rowversion – SQL Server tự tăng mỗi lần cập nhật (khóa lạc quan).</summary>
    public byte[] Version { get; private set; } = null!;

    private readonly List<AppointmentHistory> _history = new();
    public IReadOnlyCollection<AppointmentHistory> History => _history;

    private Appointment() { }

    /// <summary>Đối tượng trạng thái hiện tại (mẫu State).</summary>
    private AppointmentState State => AppointmentState.For(Status);

    /// <summary>Lịch hẹn còn hiệu lực (đang chiếm khung giờ) hay đã kết thúc.</summary>
    public bool IsActive => State.IsActive;

    // ===================== Tạo lịch (UC-02) =====================

    /// <summary>
    /// Bệnh nhân đặt lịch: giữ khung giờ, tạo lịch ở trạng thái «Chờ xác nhận» (BR-02), ghi lịch sử.
    /// Yêu cầu slot.Schedule.Doctor đã được nạp.
    /// </summary>
    public static Appointment Book(Patient patient, TimeSlot slot, PriorityLevel defaultPriority, string? reason, int byUserId)
    {
        if (slot.Schedule?.Doctor is null)
            throw new InvalidOperationException("Cần nạp Schedule.Doctor của khung giờ trước khi đặt lịch.");
        if (reason is { Length: > 500 }) throw new DomainException("Lý do khám tối đa 500 ký tự.");

        slot.Book(); // ném 409 nếu khung giờ đã bị đặt

        var a = new Appointment
        {
            Patient = patient,
            PatientId = patient.Id,
            Doctor = slot.Schedule.Doctor,
            DoctorId = slot.Schedule.DoctorId,
            Slot = slot,
            SlotId = slot.Id,
            Priority = defaultPriority,
            PriorityId = defaultPriority.Id,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        a.AddHistory(byUserId, ChangeType.Create, null, a.Status, a.Reason);
        return a;
    }

    // ===================== Các thao tác công khai =====================
    // Mỗi thao tác: nhớ trạng thái cũ -> ủy quyền cho State -> ghi lịch sử.

    public void Confirm(Staff staff, PriorityLevel priority, int byUserId)
        => Run(byUserId, ChangeType.Confirm, $"Mức ưu tiên: {priority.Name}", s => s.Confirm(this, staff, priority));

    public void Reject(string reason, int byUserId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Từ chối lịch hẹn bắt buộc nhập lý do.");
        Run(byUserId, ChangeType.Reject, reason, s => s.Reject(this));
    }

    public void Cancel(string? reason, int byUserId)
        => Run(byUserId, ChangeType.Cancel, reason, s => s.Cancel(this));

    public void Reschedule(TimeSlot newSlot, string? reason, int byUserId)
    {
        var oldStart = Slot.StartAt;
        Run(byUserId, ChangeType.Reschedule,
            $"Đổi từ {oldStart:HH:mm dd/MM/yyyy} sang {newSlot.StartAt:HH:mm dd/MM/yyyy}. {reason}".Trim(),
            s => s.Reschedule(this, newSlot));
    }

    public void CheckIn(int byUserId) => Run(byUserId, ChangeType.CheckIn, null, s => s.CheckIn(this));
    public void StartExam(int byUserId) => Run(byUserId, ChangeType.StartExam, null, s => s.StartExam(this));
    public void Complete(int byUserId) => Run(byUserId, ChangeType.Complete, null, s => s.Complete(this));
    public void MarkNoShow(string? reason, int byUserId) => Run(byUserId, ChangeType.NoShow, reason, s => s.MarkNoShow(this));

    /// <summary>Kiểm tra thời hạn được phép hủy (BR-08: tham số cấu hình, không cố định).</summary>
    public void EnsureCancellableByPatient(DateTimeOffset now, TimeSpan deadline)
    {
        if (Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new DomainException($"Không thể hủy lịch đang ở trạng thái «{StatusText.Vi(Status)}».");
        if (Slot.StartAt - now < deadline)
            throw new DomainException($"Chỉ được hủy lịch trước giờ khám ít nhất {deadline.TotalHours:0.#} giờ.");
    }

    public bool CanPatientCancel(DateTimeOffset now, TimeSpan deadline)
        => Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed && Slot.StartAt - now >= deadline;

    // ===================== Hàm nội bộ cho các lớp State =====================

    internal void SetStatus(AppointmentStatus status) => Status = status;

    internal void ApplyConfirm(Staff staff, PriorityLevel priority)
    {
        ConfirmedByStaff = staff;
        ConfirmedBy = staff.Id;
        Priority = priority;
        PriorityId = priority.Id;
        Status = AppointmentStatus.Confirmed;
    }

    internal void ReleaseSlotAndEnd(AppointmentStatus endStatus)
    {
        Slot.Release(); // BR-04: hủy / vắng mặt phải nhả khung giờ
        Status = endStatus;
    }

    internal void MoveTo(TimeSlot newSlot)
    {
        if (newSlot.Schedule?.Doctor is null)
            throw new InvalidOperationException("Cần nạp Schedule.Doctor của khung giờ mới.");
        if (ReferenceEquals(newSlot, Slot) || (newSlot.Id != 0 && newSlot.Id == SlotId))
            throw new DomainException("Khung giờ mới trùng khung giờ hiện tại.");

        newSlot.Book();   // giữ giờ mới trước (ném 409 nếu đã bị đặt)
        Slot.Release();   // rồi mới nhả giờ cũ
        Slot = newSlot;
        SlotId = newSlot.Id;
        Doctor = newSlot.Schedule.Doctor;
        DoctorId = newSlot.Schedule.DoctorId;
    }

    private void Run(int byUserId, ChangeType type, string? reason, Action<AppointmentState> action)
    {
        var old = Status;
        action(State);
        AddHistory(byUserId, type, old, Status, reason);
    }

    private void AddHistory(int byUserId, ChangeType type, AppointmentStatus? oldStatus, AppointmentStatus? newStatus, string? reason)
        => _history.Add(new AppointmentHistory(this, byUserId, type, oldStatus, newStatus, reason));
}