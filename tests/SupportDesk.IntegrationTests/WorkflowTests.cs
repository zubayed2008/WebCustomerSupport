using System.Net;
using System.Text.Json;

namespace SupportDesk.IntegrationTests;

public sealed class WorkflowTests : IClassFixture<SupportDeskApiFactory>
{
    private readonly SupportDeskApiFactory _factory;

    public WorkflowTests(SupportDeskApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AgentFlow_CreatesCommentsRecordsFirstResponseAndResolves()
    {
        using var session = new ApiSession(_factory);
        await session.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var createResponse = await session.PostAsync("/api/v1/tickets", new
        {
            subject = "Workflow verification",
            description = "Verify the full ticket workflow through the API.",
            priority = "High",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement;
        var ticketId = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(created.GetProperty("number").GetString()));
        Assert.NotNull(created.GetProperty("firstResponseDueAt").GetString());

        var commentResponse = await session.PostAsync($"/api/v1/tickets/{ticketId}/comments", new
        {
            body = "We are investigating and will update you shortly.",
            isInternal = false,
        });
        commentResponse.EnsureSuccessStatusCode();
        var afterComment = JsonDocument.Parse(await commentResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Open", afterComment.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterComment.GetProperty("firstRespondedAt").ValueKind);

        var resolveResponse = await session.PostAsync($"/api/v1/tickets/{ticketId}/transitions", new { status = "Resolved" });
        resolveResponse.EnsureSuccessStatusCode();
        var resolved = JsonDocument.Parse(await resolveResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Resolved", resolved.GetProperty("status").GetString());

        var reopenResponse = await session.PostAsync($"/api/v1/tickets/{ticketId}/transitions", new { status = "Open" });
        reopenResponse.EnsureSuccessStatusCode();
        var reopened = JsonDocument.Parse(await reopenResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Open", reopened.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("resolvedAt").ValueKind);
    }

    [Fact]
    public async Task AttachmentUpload_RejectsDisallowedContentType()
    {
        using var session = new ApiSession(_factory);
        await session.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var createResponse = await session.PostAsync("/api/v1/tickets", new
        {
            subject = "Attachment validation",
            description = "Verify upload validation is enforced.",
            priority = "Normal",
        });
        createResponse.EnsureSuccessStatusCode();
        var ticketId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();

        var token = await session.GetAntiforgeryTokenAsync();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("MZ not a real executable"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "payload.exe");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/tickets/{ticketId}/attachments") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await session.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task KnowledgeArticle_LifecycleRespectsPublicationState()
    {
        using var admin = new ApiSession(_factory);
        await admin.LoginAndEnsureSuccessAsync(SupportDeskApiFactory.AdminEmail, SupportDeskApiFactory.AdminPassword);

        var createResponse = await admin.PostAsync("/api/v1/knowledge", new
        {
            title = "Integration test article",
            summary = "Created by the integration suite.",
            body = "This article verifies the draft and publish workflow.",
            visibility = "Customer",
        });
        createResponse.EnsureSuccessStatusCode();
        var articleId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();

        using var customer = new ApiSession(_factory);
        await customer.LoginAndEnsureSuccessAsync("customer@example.com", SupportDeskApiFactory.DemoPassword);

        var draftHidden = await customer.Client.GetAsync($"/api/v1/knowledge/{articleId}");
        Assert.Equal(HttpStatusCode.Forbidden, draftHidden.StatusCode);

        var publishResponse = await admin.PostAsync($"/api/v1/knowledge/{articleId}/publish", new { });
        publishResponse.EnsureSuccessStatusCode();

        var publishedVisible = await customer.Client.GetAsync($"/api/v1/knowledge/{articleId}");
        Assert.Equal(HttpStatusCode.OK, publishedVisible.StatusCode);
    }
}
