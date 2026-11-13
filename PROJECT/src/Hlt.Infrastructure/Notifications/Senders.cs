using Hlt.Domain;
using Hlt.Domain.Notifications;
using Hlt.Domain.Users;
using Hlt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hlt.Infrastructure.Notifications;

// ---- Các cách thực thi của INotificationSender (đa hình) ----
// Bản đồ án dùng bản "giả lập" ghi ra log. Khi cần gửi thật, viết lớp SmtpEmailSender (MailKit)
// hoặc lớp gọi API SMS rồi đăng ký DI thay cho lớp tương ứng – không phải sửa chỗ nào khác.

internal class LogEmailSender(ILogger<LogEmailSender> log) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public Task SendAsync(Notification n, AppUser recipient, CancellationToken ct)
    {
        log.LogInformation("[EMAIL] Gửi tới {Email}: {Content}", recipient.Email, n.Content);
        return Task.CompletedTask;
    }
}

internal class LogSmsSender(ILogger<LogSmsSender> log) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task SendAsync(Notification n, AppUser recipient, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recipient.Phone)) throw new InvalidOperationException("Người nhận chưa có số điện thoại.");
        log.LogInformation("[SMS] Gửi tới {Phone}: {Content}", recipient.Phone, n.Content);
        return Task.CompletedTask;
    }
}

/// <summary>Thông báo trong ứng dụng: đã nằm sẵn trong bảng notification, giao diện tự đọc.</summary>
internal class InAppSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;
    public Task SendAsync(Notification n, AppUser recipient, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// Dịch vụ nền: cứ 5 giây lấy các thông báo PENDING, gửi qua đúng kênh rồi cập nhật SENT/FAILED (mục 4.3.4).
/// </summary>
internal class NotificationDispatcher(IServiceScopeFactory scopes, ILogger<NotificationDispatcher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Lỗi khi gửi thông báo");
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var senders = scope.ServiceProvider.GetServices<INotificationSender>().ToDictionary(s => s.Channel);

        var batch = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.Id).Take(20).ToListAsync(ct);
        if (batch.Count == 0) return;

        var userIds = batch.Select(n => n.UserId).Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        foreach (var n in batch)
        {
            try
            {
                if (!senders.TryGetValue(n.Channel, out var sender))
                    throw new InvalidOperationException($"Chưa có lớp gửi cho kênh {n.Channel}.");
                await sender.SendAsync(n, users[n.UserId], ct);
                n.MarkSent(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Gửi thông báo {Id} thất bại", n.Id);
                n.MarkFailed();
            }
        }
        await db.SaveChangesAsync(ct);
    }
}