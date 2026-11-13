using Hlt.Domain.Appointments;
using Hlt.Domain.Users;

namespace Hlt.Domain.Notifications;

/// <summary>
/// Thông báo gửi đến người dùng. Được ghi ở trạng thái PENDING cùng giao dịch với thay đổi lịch,
/// sau đó dịch vụ nền gửi đi và cập nhật SENT/FAILED (giúp không mất thông báo khi kênh gửi lỗi).
/// </summary>
public class Notification
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int? AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Content { get; private set; } = null!;
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    private Notification() { }

    public Notification(int userId, Appointment? appointment, NotificationChannel channel, string content)
    {
        UserId = userId;
        Appointment = appointment;
        AppointmentId = appointment?.Id is > 0 ? appointment.Id : null;
        Channel = channel;
        Content = content.Length > 1000 ? content[..1000] : content;
        Status = NotificationStatus.Pending;
    }

    public void MarkSent(DateTimeOffset now) { Status = NotificationStatus.Sent; SentAt = now; }
    public void MarkFailed() => Status = NotificationStatus.Failed;
}

/// <summary>
/// «Người gửi thông báo» (giao diện). Gửi Email, Gửi SMS là các cách thực thi cụ thể (đa hình).
/// Thêm kênh mới (Zalo, push...) chỉ cần thêm một lớp cài đặt.
/// </summary>
public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(Notification notification, AppUser recipient, CancellationToken ct = default);
}