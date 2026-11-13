namespace Hlt.Domain;

// Các enum được lưu xuống SQL Server dạng chữ HOA_CÓ_GẠCH (PENDING, CHECKED_IN...)
// nhờ bộ chuyển đổi trong lớp Infrastructure.

public enum UserRole { Patient, Doctor, Staff, Admin }

public enum AccountStatus { Active, Locked }

public enum SlotStatus { Available, Booked, Blocked }

public enum AppointmentStatus
{
    Pending,     // Chờ xác nhận
    Confirmed,   // Đã xác nhận
    CheckedIn,   // Đã check-in
    InProgress,  // Đang khám
    Completed,   // Hoàn tất
    Cancelled,   // Đã hủy
    NoShow       // Vắng mặt
}

public enum TicketStatus { Waiting, Called, Skipped, Done }

public enum NotificationChannel { Email, Sms, InApp }

public enum NotificationStatus { Pending, Sent, Failed }

public enum ChangeType { Create, Confirm, Reject, Reschedule, Cancel, CheckIn, StartExam, Complete, NoShow }

public static class StatusText
{
    public static string Vi(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Pending => "Chờ xác nhận",
        AppointmentStatus.Confirmed => "Đã xác nhận",
        AppointmentStatus.CheckedIn => "Đã check-in",
        AppointmentStatus.InProgress => "Đang khám",
        AppointmentStatus.Completed => "Hoàn tất",
        AppointmentStatus.Cancelled => "Đã hủy",
        AppointmentStatus.NoShow => "Vắng mặt",
        _ => s.ToString()
    };

    public static string Vi(TicketStatus s) => s switch
    {
        TicketStatus.Waiting => "Đang chờ",
        TicketStatus.Called => "Đang gọi",
        TicketStatus.Skipped => "Bỏ qua",
        TicketStatus.Done => "Hoàn tất",
        _ => s.ToString()
    };
}