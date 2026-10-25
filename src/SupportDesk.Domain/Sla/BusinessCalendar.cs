using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Sla;

public sealed class BusinessCalendar : Entity
{
    public string Name { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "UTC";

    public WeeklySchedule Schedule { get; set; } = WeeklySchedule.StandardWeek();

    public bool IsActive { get; set; } = true;

    public ICollection<Holiday> Holidays { get; set; } = [];

    public bool IsHoliday(DateOnly localDate)
    {
        return Holidays.Any(x => x.Date == localDate);
    }

    public bool HasWorkingIntervals(DayOfWeek day)
    {
        return Schedule.IntervalsFor(day).Count > 0;
    }
}

public sealed class Holiday : Entity
{
    public Guid BusinessCalendarId { get; set; }

    public BusinessCalendar? BusinessCalendar { get; set; }

    public DateOnly Date { get; set; }

    public string Name { get; set; } = string.Empty;
}
