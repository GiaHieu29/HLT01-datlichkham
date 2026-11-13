using Hlt.Domain.Common;

namespace Hlt.Domain.Users;

/// <summary>Tài khoản đăng nhập (bảng app_user), liên kết với danh tính OpenID Connect.</summary>
public class AppUser
{
    public int Id { get; private set; }
    public string OidcSubject { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AppUser() { } // dành cho EF Core

    public AppUser(string oidcSubject, string fullName, string email, UserRole role, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(oidcSubject)) throw new DomainException("Thiếu định danh OpenID Connect.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Họ tên không được để trống.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email không được để trống.");

        OidcSubject = oidcSubject;
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone;
        Role = role;
        Status = AccountStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => Status == AccountStatus.Active;

    public void ChangeRole(UserRole role) => Role = role;
    public void Lock() => Status = AccountStatus.Locked;
    public void Unlock() => Status = AccountStatus.Active;

    public void UpdateContact(string fullName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Họ tên không được để trống.");
        FullName = fullName.Trim();
        Phone = phone;
    }
}