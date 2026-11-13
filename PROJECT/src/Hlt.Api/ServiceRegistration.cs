using Hlt.Application;
using Hlt.Application.Services;
using Hlt.Domain.Queueing;

namespace Hlt.Api;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, BookingRules rules)
    {
        services.AddSingleton(rules);

        // Strategy: đổi cách xếp hàng chỉ bằng cấu hình "BookingRules:QueueStrategy"
        IQueueStrategy strategy = rules.QueueStrategy == "FirstCome"
            ? new FirstComeFirstServedStrategy()
            : new ClinicalPriorityStrategy();
        services.AddSingleton(strategy);
        services.AddSingleton<QueueManager>();

        // Observer: các lớp quan sát thay đổi lịch hẹn
        services.AddScoped<IAppointmentObserver, NotificationObserver>();
        services.AddScoped<AppointmentEvents>();

        // Dịch vụ theo use case
        services.AddScoped<CatalogService>();
        services.AddScoped<BookingService>();
        services.AddScoped<StaffService>();
        services.AddScoped<QueueService>();
        services.AddScoped<AdminService>();
        services.AddScoped<AccountService>();
        return services;
    }
}