using Hlt.Domain.Appointments;

namespace Hlt.Domain.Queueing;

/// <summary>
/// «Quản lý hàng đợi»: xếp bệnh nhân vào hàng khi check-in và chọn người kế tiếp khi gọi số.
/// Phụ thuộc vào giao diện IQueueStrategy chứ không phụ thuộc cài đặt cụ thể (Dependency Inversion).
/// </summary>
public class QueueManager(IQueueStrategy strategy)
{
    public IQueueStrategy Strategy { get; } = strategy;

    /// <summary>UC-10: check-in lịch hẹn và cấp số thứ tự.</summary>
    public QueueTicket CheckIn(Appointment appointment, int departmentId, DateOnly date, int ticketNo, DateTimeOffset now, int byUserId)
    {
        appointment.CheckIn(byUserId); // State kiểm tra: chỉ «Đã xác nhận» mới được check-in
        return new QueueTicket(appointment, departmentId, date, ticketNo, now);
    }

    /// <summary>UC-13: chọn người tiếp theo theo chiến lược, chuyển số sang «Đang gọi», lịch sang «Đang khám».</summary>
    public QueueTicket? CallNext(IEnumerable<QueueTicket> tickets, DateTimeOffset now, int byUserId)
    {
        var next = Strategy.SelectNext(tickets.Where(t => t.Status == TicketStatus.Waiting));
        if (next is null) return null;

        next.Call(now);
        next.Appointment.StartExam(byUserId);
        return next;
    }

    /// <summary>Bác sĩ bấm «Bỏ qua» khi bệnh nhân không có mặt.</summary>
    public void Skip(QueueTicket ticket, int byUserId)
    {
        ticket.Skip();
        ticket.Appointment.MarkNoShow("Không có mặt khi được gọi", byUserId);
    }

    /// <summary>UC-14: hoàn tất khám.</summary>
    public void Complete(QueueTicket ticket, int byUserId)
    {
        ticket.Finish();
        ticket.Appointment.Complete(byUserId);
    }

    /// <summary>Số người đang chờ trước một số thứ tự (FR-16).</summary>
    public int PeopleAhead(IEnumerable<QueueTicket> tickets, QueueTicket mine)
    {
        if (mine.Status != TicketStatus.Waiting) return 0;
        var ordered = Strategy.Order(tickets.Where(t => t.Status == TicketStatus.Waiting)).ToList();
        var index = ordered.IndexOf(mine);
        return index < 0 ? 0 : index;
    }
}