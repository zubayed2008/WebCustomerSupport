using SupportDesk.Domain;
using SupportDesk.Domain.Sla;

namespace SupportDesk.UnitTests;

public sealed class SlaCalculatorTests
{
    private static BusinessCalendar CreateCalendar(string timeZoneId = "UTC")
    {
        return new BusinessCalendar
        {
            Name = "Test calendar",
            TimeZoneId = timeZoneId,
            Schedule = WeeklySchedule.StandardWeek(),
        };
    }

    [Fact]
    public void AddBusinessMinutes_StaysInsideWorkingHours()
    {
        var calendar = CreateCalendar();
        var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        var result = SlaCalculator.AddBusinessMinutes(start, 120, calendar);

        Assert.Equal(new DateTimeOffset(2026, 1, 5, 11, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddBusinessMinutes_SkipsWeekend()
    {
        var calendar = CreateCalendar();
        // Friday 16:00 + 120 business minutes = Monday 10:00.
        var start = new DateTimeOffset(2026, 1, 2, 16, 0, 0, TimeSpan.Zero);

        var result = SlaCalculator.AddBusinessMinutes(start, 120, calendar);

        Assert.Equal(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddBusinessMinutes_SkipsHoliday()
    {
        var calendar = CreateCalendar();
        calendar.Holidays.Add(new Holiday { Date = new DateOnly(2026, 1, 5), Name = "Holiday" });
        var start = new DateTimeOffset(2026, 1, 2, 16, 0, 0, TimeSpan.Zero);

        var result = SlaCalculator.AddBusinessMinutes(start, 120, calendar);

        Assert.Equal(new DateTimeOffset(2026, 1, 6, 10, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void AddBusinessMinutes_HandlesZero()
    {
        var calendar = CreateCalendar();
        var start = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(start, SlaCalculator.AddBusinessMinutes(start, 0, calendar));
    }

    [Fact]
    public void AddBusinessMinutes_HandlesDaylightSavingTransition()
    {
        var calendar = CreateCalendar("America/New_York");
        // 2026-03-06 is a Friday. Start at 16:00 local (EST, UTC-5) and add 120 minutes.
        // The result must land on Monday 2026-03-09 at 10:00 local (EDT, UTC-4).
        var start = new DateTimeOffset(2026, 3, 6, 16, 0, 0, TimeSpan.FromHours(-5));

        var result = SlaCalculator.AddBusinessMinutes(start, 120, calendar);
        var local = TimeZoneInfo.ConvertTime(result, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

        Assert.Equal(new DateTime(2026, 3, 9, 10, 0, 0), local.DateTime);
        Assert.Equal(TimeSpan.FromHours(-4), result.Offset);
    }

    [Fact]
    public void BusinessMinutesBetween_CountsOnlyWorkingTime()
    {
        var calendar = CreateCalendar();
        var start = new DateTimeOffset(2026, 1, 5, 16, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 6, 10, 30, 0, TimeSpan.Zero);

        var minutes = SlaCalculator.BusinessMinutesBetween(start, end, calendar);

        // Monday 16:00-17:00 = 60 minutes; Tuesday 09:00-10:30 = 90 minutes.
        Assert.Equal(150, minutes);
    }

    [Fact]
    public void CreateCommitment_UsesPolicyTargets()
    {
        var calendar = CreateCalendar();
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Normal",
            Priority = TicketPriority.Normal,
            FirstResponseMinutes = 60,
            ResolutionMinutes = 240,
            BusinessCalendar = calendar,
        };
        var created = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        var commitment = SlaCalculator.CreateCommitment(policy, calendar, created);

        Assert.Equal(created.AddMinutes(60), commitment.FirstResponseDueAt);
        Assert.Equal(created.AddMinutes(240), commitment.ResolutionDueAt);
    }
}
