using Hlt.Domain.Appointments;
using Hlt.Domain.Common;
using Hlt.Domain.Organization;

namespace Hlt.Domain.Queueing;

/// <summary>Số thứ tự – một lượt chờ trong hàng đợi của khoa theo ngày.</summary>
public class QueueTicket
{
    public int Id { get; private set; }
    public int AppointmentId { get; private set; }
    public Appointment Appointment { get; private set; } = null!;
    public int DepartmentId { get; private set; }
    public int PriorityId { get; private set; }
    public PriorityLevel Priority { get; private set; } = null!;
    public DateOnly QueueDate { get; private set; }
    public int TicketNo { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTimeOffset EnqueuedAt { get; private set; }
    public DateTimeOffset? CalledAt { get; private set; }

    private QueueTicket() { }

    internal QueueTicket(Appointment appointment, int departmentId, DateOnly queueDate, int ticketNo, DateTimeOffset now)
    {
        Appointment = appointment;
        AppointmentId = appointment.Id;
        DepartmentId = departmentId;
        Priority = appointment.Priority;      // mức ưu tiên tại thời điểm xếp hàng
        PriorityId = appointment.PriorityId;
        QueueDate = queueDate;
        TicketNo = ticketNo;
        Status = TicketStatus.Waiting;
        EnqueuedAt = now;
    }

    public void Call(DateTimeOffset now)
    {
        if (Status != TicketStatus.Waiting) throw new DomainException($"Số {TicketNo} không ở trạng thái đang chờ.");
        Status = TicketStatus.Called;
        CalledAt = now;
    }

    public void Skip()
    {
        if (Status is TicketStatus.Done or TicketStatus.Skipped) throw new DomainException($"Số {TicketNo} đã kết thúc.");
        Status = TicketStatus.Skipped;
    }

    public void Finish()
    {
        if (Status != TicketStatus.Called) throw new DomainException($"Số {TicketNo} chưa được gọi vào khám.");
        Status = TicketStatus.Done;
    }
}