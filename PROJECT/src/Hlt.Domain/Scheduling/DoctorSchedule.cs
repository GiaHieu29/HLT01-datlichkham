using Hlt.Domain.Common;
using Hlt.Domain.Users;

namespace Hlt.Domain.Scheduling;

/// <summary>Ca làm việc của bác sĩ trong một ngày; sinh ra các khung giờ khám.</summary>
public class DoctorSchedule
{
    public int Id { get; private set; }
    public int DoctorId { get; private set; }
    public Doctor Doctor { get; private set; } = null!;
    public DateOnly WorkDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public short SlotMinutes { get; private set; }
    public short? MaxPatients { get; private set; }

    private readonly List<TimeSlot> _slots = new();
    public IReadOnlyCollection<TimeSlot> Slots => _slots;

    private DoctorSchedule() { }

    public DoctorSchedule(Doctor doctor, DateOnly workDate, TimeOnly start, TimeOnly end, short slotMinutes, short? maxPatients = null)
    {
        if (end <= start) throw new DomainException("Giờ kết thúc phải sau giờ bắt đầu.");
        if (slotMinutes is < 5 or > 240) throw new DomainException("Mỗi lượt khám phải từ 5 đến 240 phút.");
        if (maxPatients is <= 0) throw new DomainException("Số bệnh nhân tối đa phải lớn hơn 0.");

        Doctor = doctor;
        DoctorId = doctor.Id;
        WorkDate = workDate;
        StartTime = start;
        EndTime = end;
        SlotMinutes = slotMinutes;
        MaxPatients = maxPatients;
    }

    /// <summary>
    /// Chia ca thành các khung giờ liên tiếp dài SlotMinutes phút.
    /// utcOffset là múi giờ của cơ sở khám (Việt Nam: +07:00).
    /// </summary>
    public IReadOnlyCollection<TimeSlot> GenerateSlots(TimeSpan utcOffset)
    {
        if (_slots.Count > 0) throw new DomainException("Ca làm việc này đã được sinh khung giờ.");

        var start = WorkDate.ToDateTime(StartTime);
        var end = WorkDate.ToDateTime(EndTime);
        for (var t = start; t.AddMinutes(SlotMinutes) <= end; t = t.AddMinutes(SlotMinutes))
        {
            if (MaxPatients.HasValue && _slots.Count >= MaxPatients.Value) break;
            _slots.Add(new TimeSlot(this,
                new DateTimeOffset(t, utcOffset),
                new DateTimeOffset(t.AddMinutes(SlotMinutes), utcOffset)));
        }
        return _slots;
    }
}