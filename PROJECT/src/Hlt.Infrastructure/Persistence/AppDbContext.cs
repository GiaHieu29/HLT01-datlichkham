using Hlt.Domain.Appointments;
using Hlt.Domain.Notifications;
using Hlt.Domain.Organization;
using Hlt.Domain.Queueing;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Hlt.Infrastructure.Persistence;

/// <summary>DbContext của EF Core – ánh xạ 12 bảng lâu dài xuống SQL Server.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Staff> StaffMembers => Set<Staff>();
    public DbSet<DoctorSchedule> Schedules => Set<DoctorSchedule>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<PriorityLevel> PriorityLevels => Set<PriorityLevel>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<AppointmentHistory> AppointmentHistories => Set<AppointmentHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Cấu hình từng bảng (Configurations.cs)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        // 2. Quy ước chung cho SQL Server: tên snake_case, enum lưu dạng chữ, không xóa dây chuyền
        SqlServerConventions.Apply(modelBuilder);
    }
}