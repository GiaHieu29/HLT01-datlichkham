using Hlt.Domain.Organization;

namespace Hlt.Domain.Users;

/// <summary>
/// Lớp trừu tượng «Người dùng» trong sơ đồ lớp. Ba lớp con Bệnh nhân, Bác sĩ, Nhân viên
/// kế thừa thông tin chung (tài khoản, họ tên) và ghi đè hành vi riêng (đa hình qua Title).
/// Mỗi lớp con vẫn lưu ở bảng riêng (patient, doctor, staff) đúng như thiết kế CSDL.
/// </summary>
public abstract class UserProfile
{
    public int Id { get; protected set; }
    public int UserId { get; protected set; }
    public AppUser User { get; protected set; } = null!;

    protected UserProfile() { }
    protected UserProfile(AppUser user) => User = user;

    public string FullName => User.FullName;

    /// <summary>Cách xưng hô hiển thị – mỗi lớp con cài đặt khác nhau.</summary>
    public abstract string Title { get; }
}

public class Patient : UserProfile
{
    public DateOnly? Dob { get; private set; }
    public string? Gender { get; private set; }
    public string? Address { get; private set; }
    public string? InsuranceNo { get; private set; }

    private Patient() { }
    public Patient(AppUser user, DateOnly? dob = null, string? gender = null, string? address = null, string? insuranceNo = null)
        : base(user)
    {
        Dob = dob; Gender = gender; Address = address; InsuranceNo = insuranceNo;
    }

    public override string Title => $"Bệnh nhân {FullName}";

    public void UpdateProfile(DateOnly? dob, string? gender, string? address, string? insuranceNo)
    {
        Dob = dob; Gender = gender; Address = address; InsuranceNo = insuranceNo;
    }
}

public class Doctor : UserProfile
{
    public int DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;
    public string? Specialty { get; private set; }
    public string? LicenseNo { get; private set; }

    private Doctor() { }
    public Doctor(AppUser user, Department department, string? specialty = null, string? licenseNo = null) : base(user)
    {
        Department = department; DepartmentId = department.Id; Specialty = specialty; LicenseNo = licenseNo;
    }
    public Doctor(AppUser user, int departmentId) : base(user) => DepartmentId = departmentId;

    public override string Title => $"BS. {FullName}";
}

public class Staff : UserProfile
{
    public int DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;
    public string? Position { get; private set; }

    private Staff() { }
    public Staff(AppUser user, Department department, string? position = null) : base(user)
    {
        Department = department; DepartmentId = department.Id; Position = position;
    }
    public Staff(AppUser user, int departmentId) : base(user) => DepartmentId = departmentId;

    public override string Title => $"NV. {FullName}";
}