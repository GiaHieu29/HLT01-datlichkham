using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Common;
using Hlt.Domain.Organization;
using Hlt.Domain.Queueing;
using Hlt.Domain.Scheduling;
using Hlt.Domain.Users;
using Xunit;

namespace Hlt.Domain.Tests;

/// <summary>Kiểm thử đơn vị cho các quy tắc nghiệp vụ trong lớp Miền (không cần CSDL).</summary>
public class DomainTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private readonly Department _noi = new("Nội tổng quát");
    private readonly PriorityLevel _normal = new("NORMAL", "Bình thường", 4);
    private readonly PriorityLevel _urgent = new("URGENT", "Khẩn", 2);

    private Doctor NewDoctor() => new(new AppUser("doc", "Trần Văn B", "b@x.vn", UserRole.Doctor), _noi);
    private static Patient NewPatient(string name) => new(new AppUser(name, name, name + "@x.vn", UserRole.Patient));
    private Staff NewStaff() => new(new AppUser("staff", "Lê Thị C", "c@x.vn", UserRole.Staff), _noi);

    private List<TimeSlot> NewSlots(Doctor d)
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var schedule = new DoctorSchedule(d, date, new TimeOnly(8, 0), new TimeOnly(10, 0), 30);
        return schedule.GenerateSlots(Vn).ToList();
    }

    [Fact]
    public void Ca_lam_viec_sinh_dung_so_khung_gio()
    {
        var slots = NewSlots(NewDoctor());
        Assert.Equal(4, slots.Count);                       // 08:00, 08:30, 09:00, 09:30
        Assert.Equal(new TimeSpan(8, 0, 0), slots[0].StartAt.TimeOfDay);
        Assert.True(slots.All(s => s.Status == SlotStatus.Available));
    }

    [Fact] // TC-01
    public void Dat_lich_thanh_cong_tao_lich_cho_xac_nhan_va_giu_khung_gio()
    {
        var slot = NewSlots(NewDoctor())[0];
        var a = Appointment.Book(NewPatient("p1"), slot, _normal, "Đau đầu", byUserId: 1);

        Assert.Equal(AppointmentStatus.Pending, a.Status);
        Assert.Equal(SlotStatus.Booked, slot.Status);
        Assert.Single(a.History);
    }

    [Fact] // BR-01
    public void Khong_the_dat_trung_mot_khung_gio()
    {
        var slot = NewSlots(NewDoctor())[0];
        Appointment.Book(NewPatient("p1"), slot, _normal, null, 1);

        var ex = Assert.Throws<DomainException>(() => Appointment.Book(NewPatient("p2"), slot, _normal, null, 2));
        Assert.Equal(DomainErrorCode.Conflict, ex.Code);
    }

    [Fact] // TC-03
    public void Huy_lich_nha_khung_gio()
    {
        var slot = NewSlots(NewDoctor())[0];
        var a = Appointment.Book(NewPatient("p1"), slot, _normal, null, 1);

        a.Cancel("Bận việc", 1);

        Assert.Equal(AppointmentStatus.Cancelled, a.Status);
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact] // mẫu State
    public void Khong_the_check_in_khi_chua_xac_nhan()
    {
        var a = Appointment.Book(NewPatient("p1"), NewSlots(NewDoctor())[0], _normal, null, 1);
        Assert.Throws<DomainException>(() => a.CheckIn(2));
    }

    [Fact] // TC-05
    public void Doi_lich_giu_gio_moi_nha_gio_cu_va_ghi_lich_su()
    {
        var slots = NewSlots(NewDoctor());
        var a = Appointment.Book(NewPatient("p1"), slots[0], _normal, null, 1);
        a.Confirm(NewStaff(), _urgent, 2);

        a.Reschedule(slots[2], "Bác sĩ bận", 2);

        Assert.Equal(AppointmentStatus.Confirmed, a.Status);
        Assert.Same(slots[2], a.Slot);
        Assert.Equal(SlotStatus.Available, slots[0].Status);
        Assert.Equal(SlotStatus.Booked, slots[2].Status);
        Assert.Equal(3, a.History.Count); // tạo, xác nhận, đổi lịch
    }

    [Fact] // TC-04, BR-07
    public void Goi_so_theo_uu_tien_cung_muc_thi_den_truoc()
    {
        var doctor = NewDoctor();
        var slots = NewSlots(doctor);
        var staff = NewStaff();
        var manager = new QueueManager(new ClinicalPriorityStrategy());
        var t0 = DateTimeOffset.Now;
        var tickets = new List<QueueTicket>();

        // Ba người bình thường check-in trước, một người «Khẩn» check-in sau cùng
        for (var i = 0; i < 4; i++)
        {
            var a = Appointment.Book(NewPatient("p" + i), slots[i], _normal, null, 1);
            a.Confirm(staff, i == 3 ? _urgent : _normal, 2);
            tickets.Add(manager.CheckIn(a, 1, DateOnly.FromDateTime(t0.Date), i + 1, t0.AddMinutes(i), 2));
        }

        Assert.Equal(4, manager.CallNext(tickets, t0, 3)!.TicketNo); // người khẩn được gọi trước
        Assert.Equal(1, manager.CallNext(tickets, t0, 3)!.TicketNo); // sau đó theo thứ tự đến
        Assert.Equal(2, manager.CallNext(tickets, t0, 3)!.TicketNo);
    }

    [Fact]
    public void Hoan_tat_kham_chuyen_lich_sang_hoan_tat()
    {
        var a = Appointment.Book(NewPatient("p1"), NewSlots(NewDoctor())[0], _normal, null, 1);
        a.Confirm(NewStaff(), _normal, 2);
        var manager = new QueueManager(new ClinicalPriorityStrategy());
        var ticket = manager.CheckIn(a, 1, DateOnly.FromDateTime(DateTime.Today), 1, DateTimeOffset.Now, 2);

        manager.CallNext([ticket], DateTimeOffset.Now, 3);
        manager.Complete(ticket, 3);

        Assert.Equal(AppointmentStatus.Completed, a.Status);
        Assert.Equal(TicketStatus.Done, ticket.Status);
    }

    [Fact]
    public void Khong_cho_huy_sat_gio_kham()
    {
        var slot = NewSlots(NewDoctor())[0];
        var a = Appointment.Book(NewPatient("p1"), slot, _normal, null, 1);
        var now = slot.StartAt.AddMinutes(-30);

        Assert.Throws<DomainException>(() => a.EnsureCancellableByPatient(now, TimeSpan.FromHours(2)));
    }
}