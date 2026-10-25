namespace SupportDesk.Domain.Sla;

public sealed class TimeInterval
{
    public TimeOnly Start { get; set; }

    public TimeOnly End { get; set; }

    public TimeInterval()
    {
    }

    public TimeInterval(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
        {
            throw new ArgumentException("Interval end must be after start.", nameof(end));
        }

        Start = start;
        End = end;
    }
}

public sealed class WorkingDay
{
    public DayOfWeek Day { get; set; }

    public List<TimeInterval> Intervals { get; set; } = [];
}

/// <summary>
/// Weekly business hours. Stored as an owned JSON document on the calendar.
/// </summary>
public sealed class WeeklySchedule
{
    public List<WorkingDay> Days { get; set; } = [];

    public IReadOnlyList<TimeInterval> IntervalsFor(DayOfWeek day)
    {
        return Days.FirstOrDefault(x => x.Day == day)?.Intervals ?? [];
    }

    public static WeeklySchedule StandardWeek()
    {
        return new WeeklySchedule
        {
            Days =
            [
                new WorkingDay { Day = DayOfWeek.Monday, Intervals = [new(new TimeOnly(9, 0), new TimeOnly(17, 0))] },
                new WorkingDay { Day = DayOfWeek.Tuesday, Intervals = [new(new TimeOnly(9, 0), new TimeOnly(17, 0))] },
                new WorkingDay { Day = DayOfWeek.Wednesday, Intervals = [new(new TimeOnly(9, 0), new TimeOnly(17, 0))] },
                new WorkingDay { Day = DayOfWeek.Thursday, Intervals = [new(new TimeOnly(9, 0), new TimeOnly(17, 0))] },
                new WorkingDay { Day = DayOfWeek.Friday, Intervals = [new(new TimeOnly(9, 0), new TimeOnly(17, 0))] },
            ],
        };
    }
}
