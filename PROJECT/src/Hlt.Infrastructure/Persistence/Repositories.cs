using Hlt.Application;
using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Common;
using Hlt.Domain.Notifications;
using Hlt.Domain.Organization;
using Hlt.Domain.Queueing;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Hlt.Infrastructure.Persistence;

// Cài đặt mẫu Repository bằng EF Core. Lớp Application không biết gì về EF Core / SQL Server.

internal class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> BySubjectAsync(string oidcSubject, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.OidcSubject == oidcSubject, ct);

    public Task<AppUser?> GetAsync(int id, CancellationToken ct) => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<List<AppUser>> ListAsync(CancellationToken ct) => db.Users.OrderBy(u => u.Id).ToListAsync(ct);

    public void Add(AppUser user) => db.Users.Add(user);
}

internal class DirectoryRepository(AppDbContext db) : IDirectoryRepository
{
    public Task<List<Department>> DepartmentsAsync(CancellationToken ct) => db.Departments.OrderBy(d => d.Name).ToListAsync(ct);
    public Task<Department?> DepartmentAsync(int id, CancellationToken ct) => db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
    public void AddDepartment(Department d) => db.Departments.Add(d);

    private IQueryable<Doctor> DoctorsFull => db.Doctors.Include(d => d.User).Include(d => d.Department);

    public Task<List<Doctor>> DoctorsAsync(int? departmentId, CancellationToken ct)
        => DoctorsFull.Where(d => departmentId == null || d.DepartmentId == departmentId)
                      .Where(d => d.User.Status == AccountStatus.Active)
                      .OrderBy(d => d.User.FullName).ToListAsync(ct);

    public Task<Doctor?> DoctorAsync(int id, CancellationToken ct) => DoctorsFull.FirstOrDefaultAsync(d => d.Id == id, ct);
    public Task<Doctor?> DoctorByUserAsync(int userId, CancellationToken ct) => DoctorsFull.FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public Task<Staff?> StaffByUserAsync(int userId, CancellationToken ct)
        => db.StaffMembers.Include(s => s.User).Include(s => s.Department).FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public Task<Patient?> PatientByUserAsync(int userId, CancellationToken ct)
        => db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public void AddPatient(Patient p) => db.Patients.Add(p);
    public void AddDoctor(Doctor d) => db.Doctors.Add(d);
    public void AddStaff(Staff s) => db.StaffMembers.Add(s);

    public Task<List<PriorityLevel>> PrioritiesAsync(CancellationToken ct) => db.PriorityLevels.OrderBy(p => p.LevelOrder).ToListAsync(ct);
    public Task<PriorityLevel?> PriorityAsync(int id, CancellationToken ct) => db.PriorityLevels.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PriorityLevel> DefaultPriorityAsync(CancellationToken ct)
        => await db.PriorityLevels.FirstOrDefaultAsync(p => p.Code == PriorityLevel.DefaultCode, ct)
           ?? await db.PriorityLevels.OrderByDescending(p => p.LevelOrder).FirstOrDefaultAsync(ct)
           ?? throw new DomainException("Chưa cấu hình danh mục mức ưu tiên.");

    public void AddPriority(PriorityLevel p) => db.PriorityLevels.Add(p);

    /// <summary>Mức ưu tiên đã gắn vào lịch hẹn hoặc số thứ tự nào chưa (nếu có thì không được xóa).</summary>
    public async Task<bool> PriorityInUseAsync(int id, CancellationToken ct)
        => await db.Appointments.AnyAsync(a => a.PriorityId == id, ct)
           || await db.QueueTickets.AnyAsync(t => t.PriorityId == id, ct);

    public void RemovePriority(PriorityLevel p) => db.PriorityLevels.Remove(p);
}

internal class ScheduleRepository(AppDbContext db) : IScheduleRepository
{
    public Task<TimeSlot?> GetSlotAsync(int slotId, CancellationToken ct)
        => db.TimeSlots
            .Include(s => s.Schedule).ThenInclude(sc => sc.Doctor).ThenInclude(d => d.User)
            .Include(s => s.Schedule).ThenInclude(sc => sc.Doctor).ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(s => s.Id == slotId, ct);

    public Task<List<TimeSlot>> ListSlotsAsync(int doctorId, DateOnly date, CancellationToken ct)
        => db.TimeSlots.AsNoTracking()
            .Where(s => s.Schedule.DoctorId == doctorId && s.Schedule.WorkDate == date)
            .OrderBy(s => s.StartAt)
            .ToListAsync(ct);

    public Task<bool> ScheduleExistsAsync(int doctorId, DateOnly date, TimeOnly start, CancellationToken ct)
        => db.Schedules.AnyAsync(s => s.DoctorId == doctorId && s.WorkDate == date && s.StartTime == start, ct);

    public Task<List<DoctorSchedule>> ListSchedulesAsync(int doctorId, DateOnly from, DateOnly to, CancellationToken ct)
        => db.Schedules.Include(s => s.Slots)
            .Where(s => s.DoctorId == doctorId && s.WorkDate >= from && s.WorkDate <= to)
            .OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime)
            .ToListAsync(ct);

    public void AddSchedule(DoctorSchedule schedule) => db.Schedules.Add(schedule);
}

internal class AppointmentRepository(AppDbContext db, BookingRules rules) : IAppointmentRepository
{
    private IQueryable<Appointment> Full => db.Appointments
        .Include(a => a.Slot)
        .Include(a => a.Patient).ThenInclude(p => p.User)
        .Include(a => a.Doctor).ThenInclude(d => d.User)
        .Include(a => a.Doctor).ThenInclude(d => d.Department)
        .Include(a => a.Priority);

    /// <summary>Khoảng thời gian [00:00, 24:00) của một ngày theo múi giờ cơ sở khám.</summary>
    private (DateTimeOffset From, DateTimeOffset To) Day(DateOnly date)
    {
        var from = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), rules.Offset);
        return (from, from.AddDays(1));
    }

    public Task<Appointment?> GetAsync(int id, CancellationToken ct) => Full.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<Appointment>> ListForPatientAsync(int patientId, CancellationToken ct)
        => Full.Where(a => a.PatientId == patientId).OrderByDescending(a => a.Slot.StartAt).Take(100).ToListAsync(ct);

    public Task<List<Appointment>> ListForStaffAsync(AppointmentStatus? status, int? departmentId, DateOnly? date, CancellationToken ct)
    {
        var q = Full;
        if (status is { } s) q = q.Where(a => a.Status == s);
        if (departmentId is { } dep) q = q.Where(a => a.Doctor.DepartmentId == dep);
        if (date is { } d)
        {
            var (from, to) = Day(d);
            q = q.Where(a => a.Slot.StartAt >= from && a.Slot.StartAt < to);
        }
        return q.OrderBy(a => a.Slot.StartAt).Take(200).ToListAsync(ct);
    }

    public Task<List<Appointment>> SearchAsync(string keyword, DateOnly date, int? departmentId, CancellationToken ct)
    {
        var (from, to) = Day(date);
        var q = Full.Where(a => a.Slot.StartAt >= from && a.Slot.StartAt < to);
        if (departmentId is { } dep) q = q.Where(a => a.Doctor.DepartmentId == dep);
        keyword = keyword.Trim();
        if (int.TryParse(keyword, out var id)) q = q.Where(a => a.Id == id);
        else if (keyword.Length > 0) q = q.Where(a => a.Patient.User.FullName.Contains(keyword));
        return q.OrderBy(a => a.Slot.StartAt).Take(50).ToListAsync(ct);
    }

    public Task<List<Appointment>> ListForDoctorAsync(int doctorId, DateOnly date, CancellationToken ct)
    {
        var (from, to) = Day(date);
        return Full.Where(a => a.DoctorId == doctorId && a.Slot.StartAt >= from && a.Slot.StartAt < to)
                   .OrderBy(a => a.Slot.StartAt).ToListAsync(ct);
    }
    public Task<List<Appointment>> ListForDoctorRangeAsync(int doctorId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (start, _) = Day(from);
        var (_, end) = Day(to);
        return Full.Where(a => a.DoctorId == doctorId && a.Slot.StartAt >= start && a.Slot.StartAt < end)
                   .OrderBy(a => a.Slot.StartAt).Take(500).ToListAsync(ct);
    }

    public void Add(Appointment appointment) => db.Appointments.Add(appointment);
}

internal class QueueRepository(AppDbContext db) : IQueueRepository
{
    private IQueryable<QueueTicket> WithDetails(IQueryable<QueueTicket> q) => q
        .Include(t => t.Priority)
        .Include(t => t.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
        .Include(t => t.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
        .Include(t => t.Appointment).ThenInclude(a => a.Slot)
        .Include(t => t.Appointment).ThenInclude(a => a.Priority);

    public async Task<int> NextTicketNoAsync(int departmentId, DateOnly date, CancellationToken ct)
        // UPDLOCK + HOLDLOCK: khóa phạm vi (khoa, ngày) đến hết giao dịch -> hai quầy không cấp trùng số.
        => await db.Database.SqlQuery<int>($"""
            SELECT ISNULL(MAX(ticket_no), 0) + 1 AS [Value]
            FROM queue_ticket WITH (UPDLOCK, HOLDLOCK)
            WHERE department_id = {departmentId} AND queue_date = {date}
            """).SingleAsync(ct);

    public Task<List<QueueTicket>> LockWaitingAsync(int departmentId, DateOnly date, int doctorId, CancellationToken ct)
        // UPDLOCK, ROWLOCK: khóa các dòng đang chờ -> lần gọi thứ hai phải đợi lần thứ nhất xong.
        => WithDetails(db.QueueTickets.FromSql($"""
                SELECT * FROM queue_ticket WITH (UPDLOCK, ROWLOCK)
                WHERE department_id = {departmentId} AND queue_date = {date} AND status = 'WAITING'
                """))
            .Where(t => t.Appointment.DoctorId == doctorId)
            .ToListAsync(ct);

    public Task<List<QueueTicket>> ListAsync(int departmentId, DateOnly date, CancellationToken ct)
        => WithDetails(db.QueueTickets).Where(t => t.DepartmentId == departmentId && t.QueueDate == date).ToListAsync(ct);

    public Task<QueueTicket?> GetAsync(int id, CancellationToken ct)
        => WithDetails(db.QueueTickets).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<QueueTicket?> GetByAppointmentAsync(int appointmentId, CancellationToken ct)
        => WithDetails(db.QueueTickets).FirstOrDefaultAsync(t => t.AppointmentId == appointmentId, ct);

    public Task<List<QueueTicket>> CalledForDoctorAsync(int doctorId, DateOnly date, CancellationToken ct)
        => WithDetails(db.QueueTickets)
            .Where(t => t.QueueDate == date && t.Status == TicketStatus.Called && t.Appointment.DoctorId == doctorId)
            .OrderBy(t => t.CalledAt).ToListAsync(ct);

    public void Add(QueueTicket ticket) => db.QueueTickets.Add(ticket);
}

internal class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public void Add(Notification notification) => db.Notifications.Add(notification);

    public Task<List<Notification>> ListForUserAsync(int userId, int take, CancellationToken ct)
        => db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.Id).Take(take).ToListAsync(ct);
}