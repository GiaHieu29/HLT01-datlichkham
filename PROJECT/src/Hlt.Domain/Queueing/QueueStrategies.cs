namespace Hlt.Domain.Queueing;

/// <summary>
/// Mẫu Strategy: quy ước cách sắp xếp và chọn người tiếp theo trong hàng đợi.
/// Thêm cách xếp hàng mới = thêm một lớp cài đặt, không sửa QueueManager (nguyên tắc Open/Closed).
/// </summary>
public interface IQueueStrategy
{
    string Name { get; }

    /// <summary>Sắp xếp các số đang chờ theo thứ tự sẽ được gọi.</summary>
    IEnumerable<QueueTicket> Order(IEnumerable<QueueTicket> waiting);

    /// <summary>Chọn người tiếp theo (mặc định: phần tử đầu tiên sau khi sắp xếp).</summary>
    QueueTicket? SelectNext(IEnumerable<QueueTicket> waiting) => Order(waiting).FirstOrDefault();
}

/// <summary>BR-07: ưu tiên cao hơn (LevelOrder nhỏ hơn) gọi trước; cùng mức thì check-in sớm hơn gọi trước.</summary>
public class ClinicalPriorityStrategy : IQueueStrategy
{
    public string Name => "Xếp theo ưu tiên lâm sàng";

    public IEnumerable<QueueTicket> Order(IEnumerable<QueueTicket> waiting)
        => waiting.OrderBy(t => t.Priority.LevelOrder)
                  .ThenBy(t => t.EnqueuedAt)
                  .ThenBy(t => t.TicketNo);
}

/// <summary>Chiến lược đơn giản: ai đến trước gọi trước (minh họa đa hình, có thể thay thế bằng cấu hình).</summary>
public class FirstComeFirstServedStrategy : IQueueStrategy
{
    public string Name => "Đến trước gọi trước";

    public IEnumerable<QueueTicket> Order(IEnumerable<QueueTicket> waiting)
        => waiting.OrderBy(t => t.EnqueuedAt).ThenBy(t => t.TicketNo);
}