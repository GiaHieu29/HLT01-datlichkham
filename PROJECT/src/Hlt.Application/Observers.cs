using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Appointments;
using Hlt.Domain.Notifications;

namespace Hlt.Application;

/// <summary>
/// Mẫu Observer: các «người quan sát» được báo mỗi khi lịch hẹn thay đổi.
/// Hiện có NotificationObserver (tạo thông báo). Muốn thêm hành vi (ví dụ ghi log, đồng bộ hệ thống khác)
/// chỉ cần thêm một lớp cài đặt IAppointmentObserver và đăng ký DI.
/// </summary>
public interface IAppointmentObserver
{
    void OnChanged(Appointment appointment, ChangeType change, string message);
}

/// <summary>Phát sự kiện tới tất cả observer đã đăng ký.</summary>
public class AppointmentEvents(IEnumerable<IAppointmentObserver> observers)
{
    public void Publish(Appointment appointment, ChangeType change, string message)
    {
        foreach (var o in observers) o.OnChanged(appointment, change, message);
    }
}

/// <summary>
/// BR-05: mọi thay đổi đều gửi thông báo cho bệnh nhân.
/// Ghi bản ghi notification ở trạng thái PENDING trong CÙNG giao dịch với thay đổi lịch;
/// dịch vụ nền NotificationDispatcher sẽ gửi đi sau.
/// </summary>
public class NotificationObserver(INotificationRepository notifications) : IAppointmentObserver
{
    private static readonly NotificationChannel[] Channels = [NotificationChannel.InApp, NotificationChannel.Email];

    public void OnChanged(Appointment appointment, ChangeType change, string message)
    {
        var recipientUserId = appointment.Patient.UserId;
        foreach (var channel in Channels)
            notifications.Add(new Notification(recipientUserId, appointment, channel, message));
    }
}

/// <summary>Soạn nội dung thông báo tiếng Việt.</summary>
public static class Messages
{
    private static string When(Appointment a, TimeSpan offset) => a.Slot.StartAt.ToOffset(offset).ToString("HH:mm 'ngày' dd/MM/yyyy");

    public static string Booked(Appointment a, TimeSpan o) => $"Bạn đã đặt lịch khám với {a.Doctor.Title} lúc {When(a, o)}. Lịch đang chờ xác nhận.";
    public static string Confirmed(Appointment a, TimeSpan o) => $"Lịch khám lúc {When(a, o)} với {a.Doctor.Title} đã được xác nhận (mức ưu tiên: {a.Priority.Name}).";
    public static string Rejected(Appointment a, TimeSpan o, string reason) => $"Lịch khám lúc {When(a, o)} đã bị từ chối. Lý do: {reason}";
    public static string Cancelled(Appointment a, TimeSpan o, string? reason) => $"Lịch khám lúc {When(a, o)} đã bị hủy.{(string.IsNullOrWhiteSpace(reason) ? "" : " Lý do: " + reason)}";
    public static string Rescheduled(Appointment a, TimeSpan o) => $"Lịch khám của bạn đã được đổi sang {When(a, o)} với {a.Doctor.Title}.";
    public static string CheckedIn(Appointment a, int ticketNo) => $"Bạn đã check-in thành công. Số thứ tự của bạn là {ticketNo}.";
    public static string Called(Appointment a, int ticketNo) => $"Mời số {ticketNo} vào phòng khám của {a.Doctor.Title}.";
    public static string Completed(Appointment a) => $"Lượt khám với {a.Doctor.Title} đã hoàn tất. Cảm ơn bạn!";
    public static string NoShow(Appointment a, TimeSpan o) => $"Lịch khám lúc {When(a, o)} được ghi nhận vắng mặt.";
}