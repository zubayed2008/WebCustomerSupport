using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Sla;

/// <summary>
/// Pure, deterministic business-hours calculations. Callers provide the clock
/// value so the behavior is testable and independent of the current time.
/// </summary>
public static class SlaCalculator
{
    private const int MaxSearchDays = 3660;

    public static DateTimeOffset AddBusinessMinutes(DateTimeOffset start, int minutes, BusinessCalendar calendar)
    {
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes cannot be negative.");
        }

        if (minutes == 0)
        {
            return start;
        }

        var timeZone = ResolveTimeZone(calendar);
        var remaining = minutes;
        var cursor = start;

        for (var day = 0; day < MaxSearchDays; day++)
        {
            var cursorLocal = TimeZoneInfo.ConvertTime(cursor, timeZone).DateTime;
            var date = DateOnly.FromDateTime(cursorLocal);

            if (calendar.IsHoliday(date))
            {
                cursor = NextWorkingStart(date, timeZone, calendar);
                continue;
            }

            var intervals = calendar.Schedule.IntervalsFor(date.DayOfWeek)
                .OrderBy(x => x.Start)
                .ToList();

            foreach (var interval in intervals)
            {
                var startLocal = date.ToDateTime(interval.Start);
                var endLocal = date.ToDateTime(interval.End);

                if (cursorLocal >= endLocal)
                {
                    continue;
                }

                var from = cursorLocal > startLocal ? cursorLocal : startLocal;
                if (from >= endLocal)
                {
                    continue;
                }

                var available = (int)Math.Floor((endLocal - from).TotalMinutes);
                if (remaining <= available)
                {
                    return ToOffset(from.AddMinutes(remaining), timeZone);
                }

                remaining -= available;
            }

            cursor = NextWorkingStart(date, timeZone, calendar);
        }

        throw new DomainException("The business calendar does not contain enough working time to calculate the deadline.");
    }

    public static int BusinessMinutesBetween(DateTimeOffset start, DateTimeOffset end, BusinessCalendar calendar)
    {
        if (end <= start)
        {
            return 0;
        }

        var timeZone = ResolveTimeZone(calendar);
        var total = 0;
        var cursor = start;

        for (var day = 0; day < MaxSearchDays && cursor < end; day++)
        {
            var cursorLocal = TimeZoneInfo.ConvertTime(cursor, timeZone).DateTime;
            var date = DateOnly.FromDateTime(cursorLocal);

            if (!calendar.IsHoliday(date))
            {
                foreach (var interval in calendar.Schedule.IntervalsFor(date.DayOfWeek).OrderBy(x => x.Start))
                {
                    var intervalStart = ToOffset(date.ToDateTime(interval.Start), timeZone);
                    var intervalEnd = ToOffset(date.ToDateTime(interval.End), timeZone);
                    var overlapStart = intervalStart > cursor ? intervalStart : cursor;
                    var overlapEnd = intervalEnd < end ? intervalEnd : end;

                    if (overlapEnd > overlapStart)
                    {
                        total += (int)Math.Floor((overlapEnd - overlapStart).TotalMinutes);
                    }
                }
            }

            cursor = NextWorkingStart(date, timeZone, calendar);
        }

        return total;
    }

    public static SlaCommitment CreateCommitment(
        SlaPolicy policy,
        BusinessCalendar calendar,
        DateTimeOffset createdAt)
    {
        return new SlaCommitment(
            policy.Id,
            AddBusinessMinutes(createdAt, policy.FirstResponseMinutes, calendar),
            AddBusinessMinutes(createdAt, policy.ResolutionMinutes, calendar));
    }

    private static DateTimeOffset NextWorkingStart(DateOnly currentDate, TimeZoneInfo timeZone, BusinessCalendar calendar)
    {
        var date = currentDate.AddDays(1);

        for (var day = 0; day < MaxSearchDays; day++)
        {
            if (!calendar.IsHoliday(date))
            {
                var intervals = calendar.Schedule.IntervalsFor(date.DayOfWeek).OrderBy(x => x.Start).ToList();
                if (intervals.Count > 0)
                {
                    return ToOffset(date.ToDateTime(intervals[0].Start), timeZone);
                }
            }

            date = date.AddDays(1);
        }

        throw new DomainException("The business calendar has no future working intervals.");
    }

    private static TimeZoneInfo ResolveTimeZone(BusinessCalendar calendar)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException($"Unknown business calendar time zone '{calendar.TimeZoneId}'.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new DomainException($"Invalid business calendar time zone '{calendar.TimeZoneId}'.");
        }
    }

    private static DateTimeOffset ToOffset(DateTime local, TimeZoneInfo timeZone)
    {
        if (timeZone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        var offset = timeZone.IsAmbiguousTime(local)
            ? timeZone.GetAmbiguousTimeOffsets(local).Max()
            : timeZone.GetUtcOffset(local);

        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset);
    }
}
