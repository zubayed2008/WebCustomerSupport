using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Application.Contracts;

public sealed record SlaPolicyDto(
    Guid Id,
    string Name,
    string Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int DueSoonMinutes,
    int Version,
    bool IsActive,
    Guid BusinessCalendarId,
    string BusinessCalendarName);

public sealed record SaveSlaPolicyRequest(
    [Required, MaxLength(150)] string Name,
    string Priority,
    [Range(1, 100000)] int FirstResponseMinutes,
    [Range(1, 100000)] int ResolutionMinutes,
    [Range(1, 10000)] int DueSoonMinutes,
    Guid BusinessCalendarId,
    bool IsActive = true);

public sealed record BusinessCalendarDto(
    Guid Id,
    string Name,
    string TimeZoneId,
    bool IsActive,
    IReadOnlyList<WorkingDayDto> WorkingDays,
    IReadOnlyList<HolidayDto> Holidays);

public sealed record WorkingDayDto(string Day, IReadOnlyList<TimeIntervalDto> Intervals);

public sealed record TimeIntervalDto(string Start, string End);

public sealed record HolidayDto(Guid Id, DateOnly Date, string Name);

public sealed record SaveBusinessCalendarRequest(
    [Required, MaxLength(150)] string Name,
    [Required, MaxLength(100)] string TimeZoneId,
    bool IsActive,
    IReadOnlyList<WorkingDayDto> WorkingDays,
    IReadOnlyList<HolidayInputDto>? Holidays);

public sealed record HolidayInputDto(DateOnly Date, [MaxLength(150)] string Name);

