using Hlt.Application.Abstractions;
using Hlt.Domain.Notifications;
using Hlt.Infrastructure.Notifications;
using Hlt.Infrastructure.Persistence;
using Hlt.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hlt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Chuỗi kết nối KHÔNG lưu trong appsettings.json (tránh đưa mật khẩu lên GitHub).
        // Chạy trên máy: tạo appsettings.Development.json từ appsettings.Development.example.json.
        // Chạy bằng Docker: lấy từ biến môi trường ConnectionStrings__Default (mật khẩu trong tệp .env).
        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Thiếu ConnectionStrings:Default. Hãy tạo src/Hlt.Api/appsettings.Development.json " +
                "từ tệp appsettings.Development.example.json hoặc đặt biến môi trường ConnectionStrings__Default.");

        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDirectoryRepository, DirectoryRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISlotHoldStore, InMemorySlotHoldStore>();

        // Đa hình: cùng giao diện INotificationSender, ba cách gửi khác nhau
        services.AddScoped<INotificationSender, LogEmailSender>();
        services.AddScoped<INotificationSender, LogSmsSender>();
        services.AddScoped<INotificationSender, InAppSender>();
        services.AddHostedService<NotificationDispatcher>();

        return services;
    }
}