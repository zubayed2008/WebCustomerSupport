namespace SupportDesk.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
