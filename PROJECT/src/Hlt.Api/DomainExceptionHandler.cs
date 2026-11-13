using Hlt.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Hlt.Api;

/// <summary>Đổi DomainException thành mã HTTP: 400 / 403 / 404 / 409 kèm thông báo tiếng Việt.</summary>
public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException ex) return false; // lỗi khác -> 500 mặc định

        var status = ex.Code switch
        {
            DomainErrorCode.NotFound => StatusCodes.Status404NotFound,
            DomainErrorCode.Conflict => StatusCodes.Status409Conflict,
            DomainErrorCode.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = "Không thực hiện được yêu cầu",
            Detail = ex.Message
        }, ct);
        return true;
    }
}