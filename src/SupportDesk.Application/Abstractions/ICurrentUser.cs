namespace SupportDesk.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }

    IReadOnlyCollection<string> Roles { get; }

    string? IpAddress { get; }

    string? CorrelationId { get; }

    bool HasPermission(string permission);
}

public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;

    public string? Email => null;

    public bool IsAuthenticated => false;

    public IReadOnlyCollection<string> Permissions => [];

    public IReadOnlyCollection<string> Roles => [];

    public string? IpAddress => null;

    public string? CorrelationId => null;

    public bool HasPermission(string permission) => false;
}
