using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application.Services;

namespace SupportDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITicketNumberGenerator, PostgresTicketNumberGenerator>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<TicketService>();
        services.AddScoped<KnowledgeService>();
        services.AddScoped<SlaService>();
        services.AddScoped<ReportService>();
        services.AddScoped<NotificationQueryService>();
        services.AddScoped<AiSuggestionService>();
        services.AddScoped<AiSuggestionProcessor>();
        services.AddScoped<OutboxProcessor>();
        services.AddScoped<SlaMonitor>();
        return services;
    }
}
