using Hlt.Application.Abstractions;
using Hlt.Domain.Common;

namespace Hlt.Application.Services;

/// <summary>Thông tin người đang đăng nhập và thông báo của họ (UC-06).</summary>
public class AccountService(IUserRepository users, IDirectoryRepository directory, INotificationRepository notifications, ICurrentUser me)
{
    public async Task<MeDto> MeAsync(CancellationToken ct)
    {
        var u = await users.GetAsync(me.UserId, ct) ?? throw DomainException.NotFound("Không tìm thấy tài khoản.");
        // Bác sĩ / nhân viên: trả kèm khoa để giao diện chọn sẵn hàng đợi
        int? departmentId = u.Role switch
        {
            Hlt.Domain.UserRole.Doctor => (await directory.DoctorByUserAsync(u.Id, ct))?.DepartmentId,
            Hlt.Domain.UserRole.Staff => (await directory.StaffByUserAsync(u.Id, ct))?.DepartmentId,
            _ => null
        };
        return new MeDto(u.Id, u.FullName, u.Email, u.Role.ToString().ToUpperInvariant(), departmentId);
    }

    public async Task<List<NotificationDto>> NotificationsAsync(CancellationToken ct)
        => (await notifications.ListForUserAsync(me.UserId, 50, ct))
            .Select(n => new NotificationDto(n.Id, n.Channel.ToString(), n.Content, n.Status.ToString(), n.SentAt, n.AppointmentId))
            .ToList();
}