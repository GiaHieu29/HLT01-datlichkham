using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Notifications;
using Hlt.Domain.Organization;
using Hlt.Domain.Queueing;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;

namespace Hlt.Application.Abstractions;

// Lớp Application chỉ biết các GIAO DIỆN dưới đây.
// Cài đặt cụ thể (EF Core + SQL Server) nằm ở Hlt.Infrastructure  -> mẫu Repository.

/// <summary>Người dùng đang gọi API (lấy từ token OpenID Connect).</summary>
public interface ICurrentUser
{
    int UserId { get; }
    UserRole Role { get; }
}

/// <summary>Đồng hồ hệ thống – tách ra để kiểm thử dễ dàng.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
    /// <summary>Ngày hiện tại theo múi giờ của cơ sở khám.</summary>
    DateOnly Today { get; }
    TimeSpan ClinicOffset { get; }
}

public interface IUnitOfWork
{
    /// <summary>Lưu mọi thay đổi trong một giao dịch. Ném DomainException(Conflict) nếu xung đột đồng thời.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Chạy một khối công việc trong giao dịch tường minh (dùng khi cần khóa dòng).</summary>
    Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<AppUser?> BySubjectAsync(string oidcSubject, CancellationToken ct = default);
    Task<AppUser?> GetAsync(int id, CancellationToken ct = default);
    Task<List<AppUser>> ListAsync(CancellationToken ct = default);
    void Add(AppUser user);
}

public interface IDirectoryRepository
{
    Task<List<Department>> DepartmentsAsync(CancellationToken ct = default);
    Task<Department?> DepartmentAsync(int id, CancellationToken ct = default);
    void AddDepartment(Department d);

    Task<List<Doctor>> DoctorsAsync(int? departmentId, CancellationToken ct = default);
    Task<Doctor?> DoctorAsync(int id, CancellationToken ct = default);
    Task<Doctor?> DoctorByUserAsync(int userId, CancellationToken ct = default);
    Task<Staff?> StaffByUserAsync(int userId, CancellationToken ct = default);
    Task<Patient?> PatientByUserAsync(int userId, CancellationToken ct = default);
    void AddPatient(Patient p);
    void AddDoctor(Doctor d);
    void AddStaff(Staff s);

    Task<List<PriorityLevel>> PrioritiesAsync(CancellationToken ct = default);
    Task<PriorityLevel?> PriorityAsync(int id, CancellationToken ct = default);
    Task<PriorityLevel> DefaultPriorityAsync(CancellationToken ct = default);
    void AddPriority(PriorityLevel p);
    Task<bool> PriorityInUseAsync(int id, CancellationToken ct = default);
    void RemovePriority(PriorityLevel p);
}

public interface IScheduleRepository
{
    /// <summary>Lấy khung giờ kèm Schedule.Doctor.User và Department (để đặt lịch).</summary>
    Task<TimeSlot?> GetSlotAsync(int slotId, CancellationToken ct = default);
    Task<List<TimeSlot>> ListSlotsAsync(int doctorId, DateOnly date, CancellationToken ct = default);
    Task<bool> ScheduleExistsAsync(int doctorId, DateOnly date, TimeOnly start, CancellationToken ct = default);
    Task<List<DoctorSchedule>> ListSchedulesAsync(int doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    void AddSchedule(DoctorSchedule schedule);
}

public interface IAppointmentRepository
{
    /// <summary>Lấy lịch hẹn kèm Slot, Patient.User, Doctor.User/Department, Priority.</summary>
    Task<Appointment?> GetAsync(int id, CancellationToken ct = default);
    Task<List<Appointment>> ListForPatientAsync(int patientId, CancellationToken ct = default);
    Task<List<Appointment>> ListForStaffAsync(AppointmentStatus? status, int? departmentId, DateOnly? date, CancellationToken ct = default);
    Task<List<Appointment>> SearchAsync(string keyword, DateOnly date, int? departmentId, CancellationToken ct = default);
    Task<List<Appointment>> ListForDoctorAsync(int doctorId, DateOnly date, CancellationToken ct = default);
    Task<List<Appointment>> ListForDoctorRangeAsync(int doctorId, DateOnly from, DateOnly to, CancellationToken ct = default);
    void Add(Appointment appointment);
}

public interface IQueueRepository
{
    /// <summary>Cấp số tiếp theo (khóa phạm vi để hai lượt check-in không lấy trùng số).</summary>
    Task<int> NextTicketNoAsync(int departmentId, DateOnly date, CancellationToken ct = default);

    /// <summary>Lấy và KHÓA các số đang chờ của bác sĩ (để hai người không gọi trùng một bệnh nhân).</summary>
    Task<List<QueueTicket>> LockWaitingAsync(int departmentId, DateOnly date, int doctorId, CancellationToken ct = default);

    Task<List<QueueTicket>> ListAsync(int departmentId, DateOnly date, CancellationToken ct = default);
    Task<QueueTicket?> GetAsync(int id, CancellationToken ct = default);
    Task<QueueTicket?> GetByAppointmentAsync(int appointmentId, CancellationToken ct = default);
    Task<List<QueueTicket>> CalledForDoctorAsync(int doctorId, DateOnly date, CancellationToken ct = default);
    void Add(QueueTicket ticket);
}

public interface INotificationRepository
{
    void Add(Notification notification);
    Task<List<Notification>> ListForUserAsync(int userId, int take, CancellationToken ct = default);
}

/// <summary>Giữ chỗ tạm vài phút khi bệnh nhân đang điền form (dữ liệu tạm thời, không lưu CSDL).</summary>
public interface ISlotHoldStore
{
    bool TryHold(int slotId, int userId, TimeSpan ttl);
    int? HolderOf(int slotId);
    void Release(int slotId);
}