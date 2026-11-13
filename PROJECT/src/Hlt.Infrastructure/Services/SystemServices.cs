using Hlt.Application;
using Hlt.Application.Abstractions;

namespace Hlt.Infrastructure.Services;

internal class SystemClock(BookingRules rules) : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
    public TimeSpan ClinicOffset => rules.Offset;
    public DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(rules.Offset).DateTime);
}

/// <summary>
/// Giữ chỗ tạm trong BỘ NHỚ (dữ liệu tạm thời – mục 3.5.1): mất khi khởi động lại cũng không sai nghiệp vụ,
/// vì quyết định cuối cùng vẫn nằm ở SQL Server (rowversion + chỉ mục duy nhất).
/// Muốn chạy nhiều máy chủ API thì thay lớp này bằng bản dùng Redis.
/// </summary>
internal class InMemorySlotHoldStore : ISlotHoldStore
{
    private readonly Dictionary<int, (int UserId, DateTimeOffset Until)> _holds = new();
    private readonly object _lock = new();

    public bool TryHold(int slotId, int userId, TimeSpan ttl)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpired(now);
            if (_holds.TryGetValue(slotId, out var h) && h.UserId != userId) return false;

            // Mỗi người chỉ giữ một khung giờ tại một thời điểm
            foreach (var key in _holds.Where(x => x.Value.UserId == userId).Select(x => x.Key).ToList())
                _holds.Remove(key);

            _holds[slotId] = (userId, now + ttl);
            return true;
        }
    }

    public int? HolderOf(int slotId)
    {
        lock (_lock)
        {
            return _holds.TryGetValue(slotId, out var h) && h.Until > DateTimeOffset.UtcNow ? h.UserId : null;
        }
    }

    public void Release(int slotId)
    {
        lock (_lock) _holds.Remove(slotId);
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var key in _holds.Where(x => x.Value.Until <= now).Select(x => x.Key).ToList())
            _holds.Remove(key);
    }
}