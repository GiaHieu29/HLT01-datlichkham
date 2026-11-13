namespace Hlt.Application;

/// <summary>
/// BR-08: các tham số nghiệp vụ đọc từ appsettings.json (mục "BookingRules"),
/// không viết cứng trong mã nguồn.
/// </summary>
public class BookingRules
{
    /// <summary>Số phút giữ chỗ tạm khi bệnh nhân đang điền form.</summary>
    public int HoldMinutes { get; set; } = 5;

    /// <summary>Bệnh nhân chỉ được hủy trước giờ khám ít nhất bao nhiêu giờ.</summary>
    public int CancelDeadlineHours { get; set; } = 2;

    /// <summary>Đến muộn quá bao nhiêu phút thì không cho check-in.</summary>
    public int MaxLateMinutes { get; set; } = 30;

    /// <summary>Múi giờ của cơ sở khám (Việt Nam = 7).</summary>
    public int UtcOffsetHours { get; set; } = 7;

    /// <summary>Chiến lược xếp hàng: "ClinicalPriority" hoặc "FirstCome".</summary>
    public string QueueStrategy { get; set; } = "ClinicalPriority";

    public TimeSpan HoldDuration => TimeSpan.FromMinutes(HoldMinutes);
    public TimeSpan CancelDeadline => TimeSpan.FromHours(CancelDeadlineHours);
    public TimeSpan MaxLate => TimeSpan.FromMinutes(MaxLateMinutes);
    public TimeSpan Offset => TimeSpan.FromHours(UtcOffsetHours);
}