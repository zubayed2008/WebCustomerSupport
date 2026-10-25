using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Permissions;

public sealed class Permission : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public static Permission Create(string name, string group, string description, bool isSystem, DateTimeOffset now)
    {
        var normalized = name.Trim().ToLowerInvariant();
        if (normalized.Length < 3 || normalized.Length > 100 || normalized.Any(char.IsWhiteSpace))
        {
            throw new ValidationFailedException("Permission names must be 3-100 characters without whitespace.");
        }

        return new Permission
        {
            Name = normalized,
            Group = group.Trim(),
            Description = description.Trim(),
            IsSystem = isSystem,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
