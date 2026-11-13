using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Common;

namespace Hlt.Application.Services;

/// <summary>Tra cứu danh mục và khung giờ trống (UC-01).</summary>
public class CatalogService(
    IDirectoryRepository directory,
    IScheduleRepository schedules,
    ISlotHoldStore holds,
    ICurrentUser me,
    IClock clock,
    BookingRules rules)
{
    public async Task<List<DepartmentDto>> DepartmentsAsync(CancellationToken ct)
        => (await directory.DepartmentsAsync(ct)).Select(d => new DepartmentDto(d.Id, d.Name, d.Location)).ToList();

    public async Task<List<DoctorDto>> DoctorsAsync(int? departmentId, CancellationToken ct)
        => (await directory.DoctorsAsync(departmentId, ct))
            .Select(d => new DoctorDto(d.Id, d.FullName, d.DepartmentId, d.Department.Name, d.Specialty))
            .ToList();

    public async Task<List<PriorityDto>> PrioritiesAsync(CancellationToken ct)
        => (await directory.PrioritiesAsync(ct))
            .Select(p => new PriorityDto(p.Id, p.Code, p.Name, p.LevelOrder, p.Description))
            .ToList();

    /// <summary>FR-03: danh sách khung giờ của bác sĩ trong ngày, kèm trạng thái hiển thị.</summary>
    public async Task<List<SlotDto>> SlotsAsync(int doctorId, DateOnly date, CancellationToken ct)
    {
        var now = clock.Now;
        var slots = await schedules.ListSlotsAsync(doctorId, date, ct);
        return slots.Select(s =>
        {
            string status;
            if (s.StartAt <= now) status = "PAST";
            else if (s.Status == SlotStatus.Booked) status = "BOOKED";
            else if (s.Status == SlotStatus.Blocked) status = "BLOCKED";
            else
            {
                var holder = holds.HolderOf(s.Id);
                status = holder is null ? "AVAILABLE" : holder == me.UserId ? "MINE" : "HELD";
            }
            return new SlotDto(s.Id, s.StartAt, s.EndAt, status);
        }).ToList();
    }

    /// <summary>Giữ chỗ tạm vài phút khi bệnh nhân bấm «Chọn» một khung giờ.</summary>
    public async Task<SlotDto> HoldAsync(int slotId, CancellationToken ct)
    {
        var slot = await schedules.GetSlotAsync(slotId, ct) ?? throw DomainException.NotFound("Không tìm thấy khung giờ.");
        if (!slot.IsAvailable || slot.StartAt <= clock.Now)
            throw DomainException.Conflict("Khung giờ này không còn trống.");
        if (!holds.TryHold(slotId, me.UserId, rules.HoldDuration))
            throw DomainException.Conflict("Khung giờ đang được người khác giữ chỗ. Vui lòng chọn giờ khác.");
        return new SlotDto(slot.Id, slot.StartAt, slot.EndAt, "MINE");
    }
}