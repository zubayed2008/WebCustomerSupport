using SupportDesk.Domain.Permissions;

namespace SupportDesk.UnitTests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_HasNoDuplicateNames()
    {
        var names = Permissions.All.Select(x => x.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Catalog_NamesAreNormalizedAndNonEmpty()
    {
        foreach (var definition in Permissions.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
            Assert.Equal(definition.Name, definition.Name.Trim().ToLowerInvariant());
            Assert.DoesNotContain(' ', definition.Name);
            Assert.False(string.IsNullOrWhiteSpace(definition.Group));
            Assert.False(string.IsNullOrWhiteSpace(definition.Description));
        }
    }

    [Fact]
    public void SystemRoles_AreStableNames()
    {
        Assert.Equal("Administrator", SystemRoles.Administrator);
        Assert.Equal("Manager", SystemRoles.Manager);
        Assert.Equal("Agent", SystemRoles.Agent);
        Assert.Equal("Customer", SystemRoles.Customer);
    }
}
