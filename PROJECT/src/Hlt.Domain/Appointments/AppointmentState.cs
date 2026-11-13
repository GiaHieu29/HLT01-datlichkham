using Hlt.Domain.Common;
using Hlt.Domain.Organization;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;

namespace Hlt.Domain.Appointments;

/// <summary>
/// Mẫu State: lớp cơ sở mặc định TỪ CHỐI mọi thao tác.
/// Mỗi trạng thái con chỉ ghi đè những thao tác hợp lệ theo Bảng 18 (bảng chuyển trạng thái).
/// </summary>
internal abstract class AppointmentState
{
    public abstract AppointmentStatus Status { get; }
    public virtual bool IsActive => true;

    public virtual void Confirm(Appointment a, Staff staff, PriorityLevel p) => throw Invalid("xác nhận");
    public virtual void Reject(Appointment a) => throw Invalid("từ chối");
    public virtual void Cancel(Appointment a) => throw Invalid("hủy");
    public virtual void Reschedule(Appointment a, TimeSlot newSlot) => throw Invalid("đổi lịch");
    public virtual void CheckIn(Appointment a) => throw Invalid("check-in");
    public virtual void StartExam(Appointment a) => throw Invalid("gọi vào khám");
    public virtual void Complete(Appointment a) => throw Invalid("hoàn tất");
    public virtual void MarkNoShow(Appointment a) => throw Invalid("đánh dấu vắng mặt");

    private DomainException Invalid(string action)
        => new($"Không thể {action} khi lịch hẹn đang ở trạng thái «{StatusText.Vi(Status)}».");

    // Các đối tượng trạng thái không giữ dữ liệu riêng nên dùng chung một thể hiện (singleton).
    private static readonly Dictionary<AppointmentStatus, AppointmentState> States = new()
    {
        [AppointmentStatus.Pending] = new PendingState(),
        [AppointmentStatus.Confirmed] = new ConfirmedState(),
        [AppointmentStatus.CheckedIn] = new CheckedInState(),
        [AppointmentStatus.InProgress] = new InProgressState(),
        [AppointmentStatus.Completed] = new FinishedState(AppointmentStatus.Completed),
        [AppointmentStatus.Cancelled] = new FinishedState(AppointmentStatus.Cancelled),
        [AppointmentStatus.NoShow] = new FinishedState(AppointmentStatus.NoShow),
    };

    public static AppointmentState For(AppointmentStatus status) => States[status];
}

/// <summary>Chờ xác nhận: được xác nhận, từ chối, hủy.</summary>
internal sealed class PendingState : AppointmentState
{
    public override AppointmentStatus Status => AppointmentStatus.Pending;
    public override void Confirm(Appointment a, Staff staff, PriorityLevel p) => a.ApplyConfirm(staff, p);
    public override void Reject(Appointment a) => a.ReleaseSlotAndEnd(AppointmentStatus.Cancelled);
    public override void Cancel(Appointment a) => a.ReleaseSlotAndEnd(AppointmentStatus.Cancelled);
}

/// <summary>Đã xác nhận: đổi lịch (tự chuyển), hủy, check-in, vắng mặt.</summary>
internal sealed class ConfirmedState : AppointmentState
{
    public override AppointmentStatus Status => AppointmentStatus.Confirmed;
    public override void Reschedule(Appointment a, TimeSlot newSlot) => a.MoveTo(newSlot); // trạng thái giữ nguyên
    public override void Cancel(Appointment a) => a.ReleaseSlotAndEnd(AppointmentStatus.Cancelled);
    public override void CheckIn(Appointment a) => a.SetStatus(AppointmentStatus.CheckedIn);
    public override void MarkNoShow(Appointment a) => a.ReleaseSlotAndEnd(AppointmentStatus.NoShow);
}

/// <summary>Đã check-in: được gọi vào khám, hoặc vắng mặt khi không có mặt.</summary>
internal sealed class CheckedInState : AppointmentState
{
    public override AppointmentStatus Status => AppointmentStatus.CheckedIn;
    public override void StartExam(Appointment a) => a.SetStatus(AppointmentStatus.InProgress);
    public override void MarkNoShow(Appointment a) => a.SetStatus(AppointmentStatus.NoShow);
}

/// <summary>Đang khám: hoàn tất, hoặc vắng mặt nếu gọi mà bệnh nhân không vào (bác sĩ bấm «Bỏ qua»).</summary>
internal sealed class InProgressState : AppointmentState
{
    public override AppointmentStatus Status => AppointmentStatus.InProgress;
    public override void Complete(Appointment a) => a.SetStatus(AppointmentStatus.Completed);
    public override void MarkNoShow(Appointment a) => a.SetStatus(AppointmentStatus.NoShow);
}

/// <summary>Các trạng thái kết thúc (Hoàn tất, Đã hủy, Vắng mặt): không cho thao tác nào.</summary>
internal sealed class FinishedState(AppointmentStatus status) : AppointmentState
{
    public override AppointmentStatus Status => status;
    public override bool IsActive => false;
}