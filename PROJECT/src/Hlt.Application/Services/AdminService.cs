using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Common;
using Hlt.Domain.Organization;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;

namespace Hlt.Application.Services;

/// <summary>Use case của quản trị viên: tài khoản (UC-15), khoa và ca làm việc (UC-16), mức ưu tiên (UC-17).</summary>
public class AdminService(
    IUserRepository users,
    IDirectoryRepository directory,
    IScheduleRepository schedules,
    IUnitOfWork uow,
    BookingRules rules)
{
    // ----- Tài khoản -----
    public async Task<List<UserDto>> UsersAsync(CancellationToken ct)
        => (await users.ListAsync(ct)).Select(ToDto).ToList();

    public async Task<UserDto> ChangeRoleAsync(int userId, ChangeRoleRequest req, CancellationToken ct)
    {
        var user = await users.GetAsync(userId, ct) ?? throw DomainException.NotFound("Không tìm thấy tài khoản.");
        if (!Enum.TryParse<UserRole>(req.Role, true, out var role)) throw new DomainException("Vai trò không hợp lệ.");

        // Bảo đảm có hồ sơ tương ứng với vai trò mới
        switch (role)
        {
            case UserRole.Doctor when await directory.DoctorByUserAsync(userId, ct) is null:
                directory.AddDoctor(new Doctor(user, await RequireDepartment(req.DepartmentId, ct)));
                break;
            case UserRole.Staff when await directory.StaffByUserAsync(userId, ct) is null:
                directory.AddStaff(new Staff(user, await RequireDepartment(req.DepartmentId, ct)));
                break;
            case UserRole.Patient when await directory.PatientByUserAsync(userId, ct) is null:
                directory.AddPatient(new Patient(user));
                break;
        }
        user.ChangeRole(role);
        await uow.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> SetLockedAsync(int userId, bool locked, CancellationToken ct)
    {
        var user = await users.GetAsync(userId, ct) ?? throw DomainException.NotFound("Không tìm thấy tài khoản.");
        if (locked) user.Lock(); else user.Unlock();
        await uow.SaveChangesAsync(ct);
        return ToDto(user);
    }

    // ----- Khoa -----
    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest req, CancellationToken ct)
    {
        var d = new Department(req.Name, req.Location);
        directory.AddDepartment(d);
        await uow.SaveChangesAsync(ct);
        return new DepartmentDto(d.Id, d.Name, d.Location);
    }

    // ----- Mức ưu tiên -----
    public async Task<PriorityDto> CreatePriorityAsync(SavePriorityRequest req, CancellationToken ct)
    {
        var p = new PriorityLevel(req.Code, req.Name, req.LevelOrder, req.Description);
        directory.AddPriority(p);
        await uow.SaveChangesAsync(ct);
        return new PriorityDto(p.Id, p.Code, p.Name, p.LevelOrder, p.Description);
    }

    public async Task<PriorityDto> UpdatePriorityAsync(int id, SavePriorityRequest req, CancellationToken ct)
    {
        var p = await directory.PriorityAsync(id, ct) ?? throw DomainException.NotFound("Không tìm thấy mức ưu tiên.");
        p.Update(req.Name, req.LevelOrder, req.Description);
        await uow.SaveChangesAsync(ct);
        return new PriorityDto(p.Id, p.Code, p.Name, p.LevelOrder, p.Description);
    }

    /// <summary>Xóa mức ưu tiên. Không cho xóa mức mặc định hoặc mức đã có lịch hẹn sử dụng.</summary>
    public async Task DeletePriorityAsync(int id, CancellationToken ct)
    {
        var p = await directory.PriorityAsync(id, ct) ?? throw DomainException.NotFound("Không tìm thấy mức ưu tiên.");
        if (p.Code == PriorityLevel.DefaultCode)
            throw DomainException.Conflict($"Không thể xóa «{p.Name}» vì đây là mức mặc định khi bệnh nhân đặt lịch.");
        if (await directory.PriorityInUseAsync(id, ct))
            throw DomainException.Conflict($"Không thể xóa «{p.Name}» vì đã có lịch hẹn dùng mức này. Bạn có thể đổi tên hoặc thứ tự thay vì xóa.");
        directory.RemovePriority(p);
        await uow.SaveChangesAsync(ct);
    }

    // ----- Ca làm việc (FR-18) -----
    public async Task<ScheduleDto> CreateScheduleAsync(CreateScheduleRequest req, CancellationToken ct)
    {
        var doctor = await directory.DoctorAsync(req.DoctorId, ct) ?? throw DomainException.NotFound("Không tìm thấy bác sĩ.");
        if (await schedules.ScheduleExistsAsync(req.DoctorId, req.WorkDate, req.StartTime, ct))
            throw DomainException.Conflict("Bác sĩ đã có ca bắt đầu vào giờ này trong ngày.");

        var schedule = new DoctorSchedule(doctor, req.WorkDate, req.StartTime, req.EndTime, req.SlotMinutes, req.MaxPatients);
        var slots = schedule.GenerateSlots(rules.Offset);
        schedules.AddSchedule(schedule);
        await uow.SaveChangesAsync(ct);
        return new ScheduleDto(schedule.Id, schedule.DoctorId, schedule.WorkDate, schedule.StartTime, schedule.EndTime, schedule.SlotMinutes, slots.Count);
    }

    public async Task<List<ScheduleDto>> SchedulesAsync(int doctorId, DateOnly from, DateOnly to, CancellationToken ct)
        => (await schedules.ListSchedulesAsync(doctorId, from, to, ct))
            .Select(s => new ScheduleDto(s.Id, s.DoctorId, s.WorkDate, s.StartTime, s.EndTime, s.SlotMinutes, s.Slots.Count))
            .ToList();

    private async Task<Department> RequireDepartment(int? id, CancellationToken ct)
    {
        if (id is null) throw new DomainException("Cần chọn khoa cho bác sĩ / nhân viên.");
        return await directory.DepartmentAsync(id.Value, ct) ?? throw DomainException.NotFound("Không tìm thấy khoa.");
    }

    private static UserDto ToDto(AppUser u) => new(u.Id, u.FullName, u.Email, u.Role.ToString().ToUpperInvariant(), u.Status.ToString().ToUpperInvariant());
}