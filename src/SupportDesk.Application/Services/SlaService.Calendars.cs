using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;

namespace SupportDesk.Application.Services;

public sealed partial class SlaService
{
    public async Task<IReadOnlyList<BusinessCalendarDto>> ListCalendarsAsync(CancellationToken cancellationToken)
    {
        RequirePermission(Permissions.SlaView);
        var calendars = await _dbContext.BusinessCalendars
            .AsNoTracking()
            .Include(x => x.Holidays)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return calendars.Select(MapCalendar).ToList();
    }

    public async Task<BusinessCalendarDto> SaveCalendarAsync(Guid? id, SaveBusinessCalendarRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.SlaManage);

        if (string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            throw new ValidationFailedException("A time zone is required.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ValidationFailedException($"Time zone '{request.TimeZoneId}' is not recognized.");
        }

        var schedule = new WeeklySchedule();
        foreach (var day in request.WorkingDays ?? [])
        {
            if (!Enum.TryParse<DayOfWeek>(day.Day, true, out var dayOfWeek))
            {
                throw new ValidationFailedException($"Working day '{day.Day}' is not valid.");
            }

            var intervals = new List<TimeInterval>();
            foreach (var interval in day.Intervals ?? [])
            {
                if (!TimeOnly.TryParse(interval.Start, CultureInfo.InvariantCulture, out var start)
                    || !TimeOnly.TryParse(interval.End, CultureInfo.InvariantCulture, out var end))
                {
                    throw new ValidationFailedException("Working intervals must use HH:mm time values.");
                }

                intervals.Add(new TimeInterval(start, end));
            }

            schedule.Days.Add(new WorkingDay { Day = dayOfWeek, Intervals = intervals });
        }

        if (schedule.Days.Count == 0)
        {
            throw new ValidationFailedException("At least one working day is required.");
        }

        var now = _clock.UtcNow;
        BusinessCalendar calendar;
        if (id is null)
        {
            calendar = new BusinessCalendar { CreatedAt = now, UpdatedAt = now };
            _dbContext.BusinessCalendars.Add(calendar);
        }
        else
        {
            calendar = await _dbContext.BusinessCalendars
                .Include(x => x.Holidays)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException("Business calendar not found.");
        }

        calendar.Name = request.Name.Trim();
        calendar.TimeZoneId = request.TimeZoneId.Trim();
        calendar.IsActive = request.IsActive;
        calendar.Schedule = schedule;
        calendar.UpdatedAt = now;

        _dbContext.Holidays.RemoveRange(calendar.Holidays);
        calendar.Holidays.Clear();
        foreach (var holiday in (request.Holidays ?? []).GroupBy(x => x.Date).Select(x => x.First()))
        {
            calendar.Holidays.Add(new Holiday
            {
                BusinessCalendarId = calendar.Id,
                Date = holiday.Date,
                Name = string.IsNullOrWhiteSpace(holiday.Name) ? "Holiday" : holiday.Name.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, id is null ? "sla.calendar.create" : "sla.calendar.update", nameof(BusinessCalendar), calendar.Id.ToString(), new { calendar.Name }, cancellationToken);
        return MapCalendar(calendar);
    }

    private static BusinessCalendarDto MapCalendar(BusinessCalendar calendar)
    {
        var days = calendar.Schedule.Days
            .OrderBy(x => x.Day)
            .Select(x => new WorkingDayDto(
                x.Day.ToString(),
                x.Intervals.OrderBy(i => i.Start).Select(i => new TimeIntervalDto(i.Start.ToString("HH:mm", CultureInfo.InvariantCulture), i.End.ToString("HH:mm", CultureInfo.InvariantCulture))).ToList()))
            .ToList();

        var holidays = calendar.Holidays
            .OrderBy(x => x.Date)
            .Select(x => new HolidayDto(x.Id, x.Date, x.Name))
            .ToList();

        return new BusinessCalendarDto(calendar.Id, calendar.Name, calendar.TimeZoneId, calendar.IsActive, days, holidays);
    }
}
