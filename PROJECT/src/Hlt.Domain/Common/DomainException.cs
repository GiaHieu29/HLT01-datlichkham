namespace Hlt.Domain.Common;

/// <summary>Loại lỗi nghiệp vụ, dùng để API đổi ra mã HTTP tương ứng.</summary>
public enum DomainErrorCode
{
    Invalid,   // 400 - dữ liệu/thao tác không hợp lệ
    NotFound,  // 404 - không tìm thấy
    Conflict,  // 409 - xung đột (ví dụ khung giờ vừa bị đặt)
    Forbidden  // 403 - không có quyền
}

/// <summary>Ngoại lệ ném ra khi vi phạm quy tắc nghiệp vụ.</summary>
public class DomainException : Exception
{
    public DomainErrorCode Code { get; }

    public DomainException(string message, DomainErrorCode code = DomainErrorCode.Invalid)
        : base(message) => Code = code;

    public static DomainException Conflict(string message) => new(message, DomainErrorCode.Conflict);
    public static DomainException NotFound(string message) => new(message, DomainErrorCode.NotFound);
    public static DomainException Forbidden(string message) => new(message, DomainErrorCode.Forbidden);
}