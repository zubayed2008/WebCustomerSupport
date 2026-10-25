using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SupportDesk.IntegrationTests;

/// <summary>
/// Cookie-aware API client that performs the anti-forgery handshake required by
/// the API, mirroring the browser flow.
/// </summary>
public sealed class ApiSession : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public ApiSession(SupportDeskApiFactory factory)
    {
        // The factory's default client persists cookies, which the anti-forgery
        // handshake and the authenticated session depend on.
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    public HttpClient Client { get; }

    public async Task<string> GetAntiforgeryTokenAsync()
    {
        var response = await Client.GetAsync("/api/v1/auth/antiforgery");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Antiforgery request failed with {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryResponse>(JsonOptions);
        return payload?.Token ?? throw new InvalidOperationException("Antiforgery token was not returned.");
    }

    public async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        var token = await GetAntiforgeryTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password }, options: JsonOptions),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await Client.SendAsync(request);
    }

    public async Task LoginAndEnsureSuccessAsync(string email, string password)
    {
        var response = await LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string path, T body)
    {
        var token = await GetAntiforgeryTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string path, T body)
    {
        var token = await GetAntiforgeryTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await Client.SendAsync(request);
    }

    public async Task<T> GetAsync<T>(string path)
    {
        var response = await Client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return value ?? throw new InvalidOperationException($"GET {path} returned no content.");
    }

    public void Dispose() => Client.Dispose();

    private sealed record AntiforgeryResponse(string Token);
}
