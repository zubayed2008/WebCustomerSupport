using System.Net;
using System.Text.Json;

namespace SupportDesk.IntegrationTests;

public sealed class SecurityTests : IClassFixture<SupportDeskApiFactory>
{
    private readonly SupportDeskApiFactory _factory;

    public SecurityTests(SupportDeskApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_IsAnonymousAndHealthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TicketsEndpoint_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_RejectsWrongPasswordWithGenericMessage()
    {
        using var session = new ApiSession(_factory);

        var response = await session.LoginAsync(SupportDeskApiFactory.AdminEmail, "WrongPassword!123");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Unexpected {(int)response.StatusCode}: {body}");
        Assert.Contains("Email or password is incorrect", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no account", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("locked out permanently", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customer_CannotReadAnotherUsersTicket()
    {
        using var admin = new ApiSession(_factory);
        await admin.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var createResponse = await admin.PostAsync("/api/v1/tickets", new
        {
            subject = "Admin-owned ticket",
            description = "This ticket belongs to the administrator account.",
            priority = "Normal",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement;
        var ticketId = created.GetProperty("id").GetString();

        using var customer = new ApiSession(_factory);
        await customer.LoginAndEnsureSuccessAsync("customer@example.com", SupportDeskApiFactory.DemoPassword);

        var forbidden = await customer.Client.GetAsync($"/api/v1/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task InternalNotes_AreHiddenFromTheRequester()
    {
        using var customer = new ApiSession(_factory);
        await customer.LoginAndEnsureSuccessAsync("customer@example.com", SupportDeskApiFactory.DemoPassword);

        var createResponse = await customer.PostAsync("/api/v1/tickets", new
        {
            subject = "Customer ticket with internal note",
            description = "Please investigate this issue.",
            priority = "Normal",
        });
        createResponse.EnsureSuccessStatusCode();
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement;
        var ticketId = created.GetProperty("id").GetString();

        using var admin = new ApiSession(_factory);
        await admin.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var noteResponse = await admin.PostAsync($"/api/v1/tickets/{ticketId}/comments", new
        {
            body = "Internal diagnostic detail that the customer must not see.",
            isInternal = true,
        });
        noteResponse.EnsureSuccessStatusCode();

        var customerView = await customer.GetAsync<JsonElement>($"/api/v1/tickets/{ticketId}");
        var adminView = await admin.GetAsync<JsonElement>($"/api/v1/tickets/{ticketId}");

        var customerComments = customerView.GetProperty("comments").EnumerateArray().ToList();
        var adminComments = adminView.GetProperty("comments").EnumerateArray().ToList();

        Assert.DoesNotContain(customerComments, comment => comment.GetProperty("visibility").GetString() == "Internal");
        Assert.Contains(adminComments, comment => comment.GetProperty("visibility").GetString() == "Internal");
    }

    [Fact]
    public async Task DynamicRolePermission_GrantsAccessWithoutRedeployment()
    {
        using var admin = new ApiSession(_factory);
        await admin.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var roleResponse = await admin.PostAsync("/api/v1/admin/roles", new { name = "Auditor" });
        roleResponse.EnsureSuccessStatusCode();
        var role = JsonDocument.Parse(await roleResponse.Content.ReadAsStringAsync()).RootElement;
        var roleId = role.GetProperty("id").GetString();

        var grantResponse = await admin.PutAsync($"/api/v1/admin/roles/{roleId}/permissions", new
        {
            permissionNames = new[] { "tickets.view.all", "reports.view.all" },
        });
        grantResponse.EnsureSuccessStatusCode();

        var userResponse = await admin.PostAsync("/api/v1/admin/users", new
        {
            email = "auditor@test.local",
            password = "Auditor!Pass123",
            displayName = "Audit User",
            roles = new[] { "Auditor" },
        });
        userResponse.EnsureSuccessStatusCode();

        using var auditor = new ApiSession(_factory);
        await auditor.LoginAndEnsureSuccessAsync("auditor@test.local", "Auditor!Pass123");

        var tickets = await auditor.GetAsync<JsonElement>("/api/v1/tickets");
        var reports = await auditor.Client.GetAsync("/api/v1/reports/overview");

        Assert.True(tickets.TryGetProperty("items", out _));
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
    }
}
