using Hlt.Domain.Appointments;
using Hlt.Domain.Notifications;
using Hlt.Domain.Organization;
using Hlt.Domain.Queueing;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hlt.Infrastructure.Persistence;

// Mỗi lớp dưới đây cấu hình một bảng theo đúng mục 3.5.4 của báo cáo.
// Lưu ý SQL Server: chuỗi tiếng Việt dùng NVARCHAR (mặc định của EF), mã/email dùng VARCHAR (IsUnicode(false)).

internal class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("app_user");
        b.Property(x => x.OidcSubject).HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.FullName).HasMaxLength(150);
        b.Property(x => x.Email).HasMaxLength(150).IsUnicode(false);
        b.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
        b.HasIndex(x => x.OidcSubject).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
    }
}

internal class DepartmentConfig : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("department");
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Location).HasMaxLength(150);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal class PatientConfig : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> b)
    {
        b.ToTable("patient");
        b.HasOne(x => x.User).WithOne().HasForeignKey<Patient>(x => x.UserId); // UK + FK app_user
        b.Property(x => x.Gender).HasMaxLength(10);
        b.Property(x => x.Address).HasMaxLength(250);
        b.Property(x => x.InsuranceNo).HasMaxLength(30).IsUnicode(false);
    }
}

internal class DoctorConfig : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> b)
    {
        b.ToTable("doctor");
        b.HasOne(x => x.User).WithOne().HasForeignKey<Doctor>(x => x.UserId);
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        b.Property(x => x.Specialty).HasMaxLength(100);
        b.Property(x => x.LicenseNo).HasMaxLength(30).IsUnicode(false);
        // SQL Server coi các NULL là trùng nhau trong chỉ mục UNIQUE -> phải lọc bỏ NULL
        b.HasIndex(x => x.LicenseNo).IsUnique().HasFilter("[license_no] IS NOT NULL");
    }
}

internal class StaffConfig : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        b.ToTable("staff");
        b.HasOne(x => x.User).WithOne().HasForeignKey<Staff>(x => x.UserId);
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        b.Property(x => x.Position).HasMaxLength(100);
    }
}

internal class DoctorScheduleConfig : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> b)
    {
        b.ToTable("doctor_schedule", t => t.HasCheckConstraint("ck_doctor_schedule_time", "[end_time] > [start_time]"));
        b.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId);
        b.HasIndex(x => new { x.DoctorId, x.WorkDate, x.StartTime }).IsUnique(); // không tạo trùng ca
        b.HasMany(x => x.Slots).WithOne(s => s.Schedule).HasForeignKey(s => s.ScheduleId);
        b.Navigation(x => x.Slots).HasField("_slots");
    }
}

internal class TimeSlotConfig : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> b)
    {
        b.ToTable("time_slot");
        b.Property(x => x.RowVersion).IsRowVersion();                       // cột rowversion -> khóa lạc quan
        b.HasIndex(x => new { x.ScheduleId, x.StartAt }).IsUnique();        // không trùng khung giờ trong ca
        b.HasIndex(x => new { x.ScheduleId, x.Status, x.StartAt });         // tìm khung giờ trống nhanh
    }
}

internal class PriorityLevelConfig : IEntityTypeConfiguration<PriorityLevel>
{
    public void Configure(EntityTypeBuilder<PriorityLevel> b)
    {
        b.ToTable("priority_level");
        b.Property(x => x.Code).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(250);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal class AppointmentConfig : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("appointment");
        b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId);
        b.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId);
        b.HasOne(x => x.Slot).WithMany().HasForeignKey(x => x.SlotId);
        b.HasOne(x => x.Priority).WithMany().HasForeignKey(x => x.PriorityId);
        b.HasOne(x => x.ConfirmedByStaff).WithMany().HasForeignKey(x => x.ConfirmedBy);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Version).IsRowVersion();

        // BR-01 / NFR-01: chỉ mục duy nhất CÓ ĐIỀU KIỆN (filtered index) của SQL Server:
        // mỗi khung giờ chỉ có tối đa một lịch hẹn đang hiệu lực.
        b.HasIndex(x => x.SlotId)
            .IsUnique()
            .HasFilter("[status] IN ('PENDING','CONFIRMED','CHECKED_IN','IN_PROGRESS')")
            .HasDatabaseName("ux_appointment_slot_active");
        b.HasIndex(x => x.Status);

        b.HasMany(x => x.History).WithOne(h => h.Appointment).HasForeignKey(h => h.AppointmentId);
        b.Navigation(x => x.History).HasField("_history");
    }
}

internal class QueueTicketConfig : IEntityTypeConfiguration<QueueTicket>
{
    public void Configure(EntityTypeBuilder<QueueTicket> b)
    {
        b.ToTable("queue_ticket");
        b.HasOne(x => x.Appointment).WithOne().HasForeignKey<QueueTicket>(x => x.AppointmentId); // UK appointment_id
        b.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
        b.HasOne(x => x.Priority).WithMany().HasForeignKey(x => x.PriorityId);
        b.HasIndex(x => new { x.DepartmentId, x.QueueDate, x.TicketNo }).IsUnique(); // BR-06
        b.HasIndex(x => new { x.DepartmentId, x.QueueDate, x.Status });              // lấy hàng đợi nhanh
    }
}

internal class AppointmentHistoryConfig : IEntityTypeConfiguration<AppointmentHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentHistory> b)
    {
        b.ToTable("appointment_history");
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ChangedBy);
        b.Property(x => x.Reason).HasMaxLength(500);
    }
}

internal class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notification");
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId);
        b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId);
        b.Property(x => x.Content).HasMaxLength(1000);
        b.HasIndex(x => x.Status);
    }
}