using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiTestingPacket.Repository.Contexts;
using ApiTestingPacket.Service.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiTestingPacket.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"tests-{Guid.NewGuid()}";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-that-is-long-enough-and-never-production");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApiTestingPacketDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApiTestingPacketDbContext>>();
            services.AddDbContext<ApiTestingPacketDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public sealed class AuthenticationAndOwnershipTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthenticationAndOwnershipTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Registration_authenticates_and_workspace_is_isolated()
    {
        using var owner = _factory.CreateClient();
        var ownerAuth = await Register(owner, "owner@example.test");
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAuth.GetProperty("accessToken").GetString());
        var create = await owner.PostAsJsonAsync("/api/collections", new { name = "Private API", description = "", sortOrder = 0 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var collectionId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/auth/me")).StatusCode);

        using var stranger = _factory.CreateClient();
        var strangerAuth = await Register(stranger, "stranger@example.test");
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", strangerAuth.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/collections/{collectionId}")).StatusCode);
        var list = await stranger.GetFromJsonAsync<JsonElement[]>("/api/collections");
        Assert.Empty(list!);
    }

    private static async Task<JsonElement> Register(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "correct horse battery staple", displayName = "Tester" });
        response.EnsureSuccessStatusCode(); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class RegistrationLifecycleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RegistrationLifecycleTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_me_refresh_logout_and_login_complete_the_authentication_lifecycle()
    {
        using var client = _factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/auth/register", Credentials("register@example.test"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var registered = await registration.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(registered.TryGetProperty("refreshToken", out _));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.GetProperty("accessToken").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("register@example.test", me.GetProperty("email").GetString());

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        Assert.True(refresh.IsSuccessStatusCode, await refresh.Content.ReadAsStringAsync());
        var refreshed = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.GetProperty("accessToken").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { })).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", Credentials("register@example.test"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn.GetProperty("accessToken").GetString());
        var postLoginRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        Assert.True(postLoginRefresh.IsSuccessStatusCode, await postLoginRefresh.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Registration_rejects_invalid_and_duplicate_accounts()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "bad", password = "short", displayName = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/register", Credentials("duplicate@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", Credentials("duplicate@example.test"))).StatusCode);
    }

    private static object Credentials(string email) => new { email, password = "correct horse battery staple", displayName = "Tester" };
}

public sealed class OpenApiImporterTests
{
    [Fact]
    public void Imports_supported_operations_without_persisting_partial_state()
    {
        const string json = """{"openapi":"3.1.0","info":{"title":"Pets","version":"1"},"servers":[{"url":"https://api.example.test"}],"paths":{"/pets":{"get":{"summary":"List pets","parameters":[{"name":"limit","in":"query","schema":{"type":"integer"}}]}}}}""";
        var result = OpenApiImporter.Parse(json, "json", null);
        var request = Assert.Single(result.Requests); Assert.Equal("Pets", result.Title); Assert.Equal("GET", request.Method); Assert.Equal("https://api.example.test/pets", request.Url); Assert.Equal("limit", Assert.Single(request.Query!).Name);
    }

    [Fact] public void Rejects_unsupported_versions() => Assert.Throws<InvalidDataException>(() => OpenApiImporter.Parse("""{"openapi":"2.0","paths":{}}""", "json", null));

    [Fact]
    public void Imports_yaml()
    {
        const string yaml = """
            openapi: 3.0.3
            info:
              title: Status API
              version: 1.0.0
            paths:
              /status:
                get:
                  operationId: getStatus
            """;
        Assert.Equal("getStatus", Assert.Single(OpenApiImporter.Parse(yaml, "yaml", null).Requests).Name);
    }
}
