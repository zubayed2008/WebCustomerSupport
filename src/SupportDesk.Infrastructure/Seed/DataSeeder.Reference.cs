using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Infrastructure.Seed;

public sealed partial class DataSeeder
{
    private async Task SeedReferenceDataAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Categories.AnyAsync(cancellationToken))
        {
            _dbContext.Categories.AddRange(
                new Category { Name = "General", Description = "General questions", SortOrder = 1, CreatedAt = now, UpdatedAt = now },
                new Category { Name = "Technical", Description = "Technical issues and errors", SortOrder = 2, CreatedAt = now, UpdatedAt = now },
                new Category { Name = "Product", Description = "Product usage questions", SortOrder = 3, CreatedAt = now, UpdatedAt = now },
                new Category { Name = "Account", Description = "Access and profile questions", SortOrder = 4, CreatedAt = now, UpdatedAt = now });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await _dbContext.BusinessCalendars.AnyAsync(cancellationToken))
        {
            _dbContext.BusinessCalendars.Add(new BusinessCalendar
            {
                Name = "Standard Business Hours (UTC)",
                TimeZoneId = "UTC",
                IsActive = true,
                Schedule = WeeklySchedule.StandardWeek(),
                CreatedAt = now,
                UpdatedAt = now,
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await _dbContext.SlaPolicies.AnyAsync(cancellationToken))
        {
            var calendar = await _dbContext.BusinessCalendars.FirstAsync(cancellationToken);
            _dbContext.SlaPolicies.AddRange(
                CreatePolicy("Urgent support", TicketPriority.Urgent, 30, 240, 15, calendar.Id, now),
                CreatePolicy("High priority support", TicketPriority.High, 60, 480, 30, calendar.Id, now),
                CreatePolicy("Normal support", TicketPriority.Normal, 240, 1440, 60, calendar.Id, now),
                CreatePolicy("Low priority support", TicketPriority.Low, 480, 2880, 120, calendar.Id, now));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static SlaPolicy CreatePolicy(string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes, int dueSoonMinutes, Guid calendarId, DateTimeOffset now)
    {
        return new SlaPolicy
        {
            Name = name,
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            DueSoonMinutes = dueSoonMinutes,
            BusinessCalendarId = calendarId,
            IsActive = true,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
