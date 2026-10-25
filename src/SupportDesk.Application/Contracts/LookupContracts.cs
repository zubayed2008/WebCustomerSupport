namespace SupportDesk.Application.Contracts;

public sealed record AssigneeDto(Guid Id, string DisplayName, string Email, IReadOnlyList<string> Roles);

public sealed record CategoryDto(Guid Id, string Name, string? Description, int SortOrder, bool IsActive);

