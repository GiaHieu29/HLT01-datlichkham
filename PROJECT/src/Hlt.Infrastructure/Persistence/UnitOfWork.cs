using Hlt.Application.Abstractions;
using Hlt.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Hlt.Infrastructure.Persistence;

/// <summary>
/// Đơn vị công việc: gói SaveChanges / giao dịch và đổi lỗi SQL Server thành lỗi nghiệp vụ dễ hiểu.
///  - DbUpdateConcurrencyException: rowversion đã đổi (ai đó vừa sửa cùng dòng)      -> 409
///  - SqlException 2601/2627: vi phạm UNIQUE (ví dụ chỉ mục ux_appointment_slot_active) -> 409
///  - SqlException 1205: deadlock, SQL Server chọn giao dịch này làm "nạn nhân"         -> 409
/// </summary>
internal class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw DomainException.Conflict("Dữ liệu vừa được người khác thay đổi (ví dụ khung giờ vừa có người đặt). Vui lòng tải lại và chọn lại.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw DomainException.Conflict("Dữ liệu bị trùng: khung giờ đã có lịch hẹn hiệu lực hoặc giá trị đã tồn tại.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 1205 })
        {
            throw DomainException.Conflict("Hệ thống đang bận, vui lòng thử lại.");
        }
    }

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null) return await work();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await work();
            await tx.CommitAsync(ct);
            return result;
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw DomainException.Conflict("Hệ thống đang bận, vui lòng thử lại.");
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}