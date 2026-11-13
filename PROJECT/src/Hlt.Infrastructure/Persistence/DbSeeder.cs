using Hlt.Application;
using Hlt.Domain;
using Hlt.Domain.Organization;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hlt.Infrastructure.Persistence;

/// <summary>Tạo CSDL (migration) và dữ liệu mẫu khi khởi động.</summary>
public static class DatabaseInitializer
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, bool seed)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rules = scope.ServiceProvider.GetRequiredService<BookingRules>();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        // SQL Server trong Docker cần vài giây để khởi động -> thử lại tối đa 10 lần
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < 10 && ex is not InvalidOperationException)
            {
                log.LogWarning("SQL Server chưa sẵn sàng (lần {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        if (seed) await DbSeeder.SeedAsync(db, rules);
    }
}

public static class DbSeeder
{
    // Các id này TRÙNG với id người dùng trong keycloak/hlt01-realm.json
    public const string PatientSub = "11111111-1111-1111-1111-111111111111";
    public const string StaffSub = "22222222-2222-2222-2222-222222222222";
    public const string DoctorSub = "33333333-3333-3333-3333-333333333333";
    public const string AdminSub = "44444444-4444-4444-4444-444444444444";
    public const string Doctor2Sub = "55555555-5555-5555-5555-555555555555";

    public static async Task SeedAsync(AppDbContext db, BookingRules rules)
    {
        if (!await db.PriorityLevels.AnyAsync())
        {
            db.PriorityLevels.AddRange(
                new PriorityLevel("EMERGENCY", "Cấp cứu", 1, "Đe dọa tính mạng, khám ngay"),
                new PriorityLevel("URGENT", "Khẩn", 2, "Cần khám sớm"),
                new PriorityLevel("PRIORITY", "Ưu tiên", 3, "Người cao tuổi, trẻ nhỏ, phụ nữ có thai"),
                new PriorityLevel("NORMAL", "Bình thường", 4, "Mặc định"));
            await db.SaveChangesAsync();
        }

        if (!await db.Departments.AnyAsync())
        {
            var noi = new Department("Khoa Nội", "Tòa A, tầng 1, phòng 101");
            var nhi = new Department("Khoa Nhi", "Tòa A, tầng 2, phòng 201");
            var rhm = new Department("Răng Hàm Mặt", "Tòa B, tầng 1, phòng 105");
            db.Departments.AddRange(noi, nhi, rhm);

            db.Patients.Add(new Patient(
                new AppUser(PatientSub, "Nguyễn Văn An", "benhnhan1@hlt.local", UserRole.Patient, "0901000001"),
                new DateOnly(1990, 5, 12), "Nam", "TP. Hồ Chí Minh", "DN4790000000001"));

            db.StaffMembers.Add(new Staff(
                new AppUser(StaffSub, "Lê Thị Hoa", "nhanvien1@hlt.local", UserRole.Staff, "0901000002"), noi, "Tiếp đón"));

            db.Doctors.AddRange(
                new Doctor(new AppUser(DoctorSub, "Trần Văn Bình", "bacsi1@hlt.local", UserRole.Doctor), noi, "Nội tổng quát", "CCHN-0001"),
                new Doctor(new AppUser(Doctor2Sub, "Phạm Thị Cúc", "bacsi2@hlt.local", UserRole.Doctor), nhi, "Nhi khoa", "CCHN-0002"));

            db.Users.Add(new AppUser(AdminSub, "Quản trị viên", "admin1@hlt.local", UserRole.Admin));
            await db.SaveChangesAsync();
        }

        // Mỗi lần khởi động: sinh ca sáng/chiều cho 7 ngày tới (ngày nào đã có ca thì bỏ qua)
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(rules.Offset).DateTime);
        var doctors = await db.Doctors.ToListAsync();
        foreach (var doctor in doctors)
        {
            for (var i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                if (await db.Schedules.AnyAsync(s => s.DoctorId == doctor.Id && s.WorkDate == date)) continue;

                var morning = new DoctorSchedule(doctor, date, new TimeOnly(7, 30), new TimeOnly(11, 30), 30);
                var afternoon = new DoctorSchedule(doctor, date, new TimeOnly(13, 30), new TimeOnly(17, 0), 30);
                morning.GenerateSlots(rules.Offset);
                afternoon.GenerateSlots(rules.Offset);
                db.Schedules.AddRange(morning, afternoon);
            }
        }
        await db.SaveChangesAsync();
    }
}