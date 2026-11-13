using System.Security.Claims;
using Hlt.Application.Abstractions;
using Hlt.Domain;
using Hlt.Domain.Common;
using Hlt.Domain.Users;
using Microsoft.AspNetCore.Authentication;

namespace Hlt.Api.Auth;

public static class Roles
{
    public const string Patient = "PATIENT";
    public const string Doctor = "DOCTOR";
    public const string Staff = "STAFF";
    public const string Admin = "ADMIN";
}

public static class AppClaims
{
    public const string UserId = "app_user_id";
    public const string Role = "role";
}

/// <summary>
/// Mục 3.8 của báo cáo: sau khi kiểm tra token, đối chiếu claim "sub" (oidc_subject) với bảng app_user
/// để lấy vai trò rồi gắn vào người dùng hiện tại -> dùng được [Authorize(Roles = "STAFF")].
/// Người đăng ký mới trên Keycloak sẽ tự được tạo tài khoản BỆNH NHÂN ở lần đăng nhập đầu tiên.
/// </summary>
public class AppUserClaimsTransformation(IUserRepository users, IDirectoryRepository directory, IUnitOfWork uow)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == AppClaims.UserId))
            return principal;

        var sub = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub)) return principal;

        var user = await users.BySubjectAsync(sub);
        if (user is null)
        {
            var name = principal.FindFirst("name")?.Value ?? principal.FindFirst("preferred_username")?.Value ?? "Người dùng mới";
            var email = principal.FindFirst("email")?.Value ?? $"{sub}@no-email.local";
            user = new AppUser(sub, name, email, UserRole.Patient);
            users.Add(user);
            directory.AddPatient(new Patient(user));
            await uow.SaveChangesAsync();
        }

        if (!user.IsActive) return principal; // tài khoản bị khóa: không có vai trò -> 403

        var identity = new ClaimsIdentity("app", "name", AppClaims.Role);
        identity.AddClaim(new Claim(AppClaims.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(AppClaims.Role, user.Role.ToString().ToUpperInvariant()));

        var clone = principal.Clone();
        clone.AddIdentity(identity);
        return clone;
    }
}

/// <summary>Cài đặt ICurrentUser bằng HttpContext.</summary>
public class HttpCurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal Principal => http.HttpContext?.User ?? new ClaimsPrincipal();

    public int UserId => int.TryParse(Principal.FindFirst(AppClaims.UserId)?.Value, out var id)
        ? id
        : throw DomainException.Forbidden("Tài khoản chưa được kích hoạt hoặc đã bị khóa.");

    public UserRole Role => Enum.TryParse<UserRole>(Principal.FindFirst(AppClaims.Role)?.Value, true, out var r)
        ? r
        : UserRole.Patient;
}