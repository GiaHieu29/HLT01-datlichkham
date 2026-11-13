using Hlt.Domain.Common;

namespace Hlt.Domain.Scheduling;

/// <summary>
/// Khung giờ khám – đơn vị đặt lịch nhỏ nhất.
/// Đóng gói: trạng thái chỉ đổi qua Book()/Release()/Block(), bên ngoài không gán trực tiếp.
/// </summary>
public class TimeSlot
{
    public int Id { get; private set; }
    public int ScheduleId { get; private set; }
    public DoctorSchedule Schedule { get; private set; } = null!;
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public SlotStatus Status { get; private set; }

    /// <summary>Cột rowversion của SQL Server – dùng cho khóa lạc quan (optimistic concurrency).</summary>
    public byte[] RowVersion { get; private set; } = null!;

    private TimeSlot() { }

    internal TimeSlot(DoctorSchedule schedule, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        Schedule = schedule;
        StartAt = startAt;
        EndAt = endAt;
        Status = SlotStatus.Available;
    }

    public bool IsAvailable => Status == SlotStatus.Available;

    /// <summary>Giữ khung giờ cho một lịch hẹn. Ném lỗi xung đột nếu đã có người đặt.</summary>
    public void Book()
    {
        if (Status != SlotStatus.Available)
            throw DomainException.Conflict("Khung giờ này vừa có người đặt hoặc không còn nhận lịch. Vui lòng chọn khung giờ khác.");
        Status = SlotStatus.Booked;
    }

    /// <summary>Nhả khung giờ (khi hủy, đổi lịch, vắng mặt) để người khác đặt được.</summary>
    public void Release()
    {
        if (Status == SlotStatus.Booked) Status = SlotStatus.Available;
    }

    public void Block()
    {
        if (Status == SlotStatus.Booked) throw DomainException.Conflict("Khung giờ đã có lịch hẹn, không thể khóa.");
        Status = SlotStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status == SlotStatus.Blocked) Status = SlotStatus.Available;
    }
}