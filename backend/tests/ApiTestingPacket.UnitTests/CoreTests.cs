using System.Net;
using System.Text;
using System.Text.Json;
using ApiTestingPacket.Core;
using ApiTestingPacket.Service.Execution;
using ApiTestingPacket.Core.DTOs.Auth;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Service.Security;
using ApiTestingPacket.Service.Services;
using Microsoft.Extensions.Options;

namespace ApiTestingPacket.UnitTests;

public sealed class PacketSerializationTests
{
    [Fact]
    public void Deserializes_environment_secret_lists_from_packet_json()
    {
        var packet = JsonSerializer.Deserialize<PacketFile>("""{"schemaVersion":1,"name":"test","requests":[],"environments":[{"name":"ci","variables":{"token":"value"},"secrets":["token"]}]}""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("token", Assert.Single(Assert.Single(packet!.Environments!).Secrets!));
    }
}

public sealed class VariableResolverTests
{
    [Fact] public void Resolves_case_insensitively() => Assert.Equal("https://example.test/users/42", VariableResolver.Resolve("{{baseUrl}}/users/{{USERid}}", new Dictionary<string, string> { ["BaseUrl"] = "https://example.test", ["userId"] = "42" }));
    [Fact] public void Runtime_values_override_environment() { var result = VariableResolver.Merge(new Dictionary<string, string> { ["token"] = "environment" }, new Dictionary<string, string> { ["token"] = "runtime" }); Assert.Equal("runtime", result["token"]); }
    [Fact] public void Missing_values_fail_with_names() { var error = Assert.Throws<VariableResolutionException>(() => VariableResolver.Resolve("{{baseUrl}}/{{id}}", new Dictionary<string, string>())); Assert.Equal(["baseUrl", "id"], error.Names); }
}

public sealed class JsonPathTests
{
    [Fact] public void Resolves_nested_arrays() { using var json = System.Text.Json.JsonDocument.Parse("""{"data":[{"user":{"id":17}}]}"""); Assert.True(JsonPath.TryResolve(json.RootElement, "data[0].user.id", out var value)); Assert.Equal(17, value.GetInt32()); }
    [Fact] public void Returns_false_for_missing_paths() { using var json = System.Text.Json.JsonDocument.Parse("{}"); Assert.False(JsonPath.TryResolve(json.RootElement, "user.id", out _)); }
}

public sealed class AssertionEngineTests
{
    private readonly AssertionEngine _engine = new();
    [Fact]
    public void Evaluates_status_json_type_array_and_format()
    {
        var definitions = new[] { new AssertionDefinition(AssertionKind.Status, AssertionOperator.Equals, Expected: "200"), new AssertionDefinition(AssertionKind.JsonValue, AssertionOperator.Equals, "user.id", "7"), new AssertionDefinition(AssertionKind.JsonType, AssertionOperator.Equals, "user.id", "integer"), new AssertionDefinition(AssertionKind.ArrayLength, AssertionOperator.GreaterThan, "items", "0"), new AssertionDefinition(AssertionKind.Format, AssertionOperator.Equals, "user.email", "email") };
        var result = _engine.Evaluate(definitions, 200, 42, new Dictionary<string, string[]>(), """{"user":{"id":7,"email":"dev@example.test"},"items":[1]}""");
        Assert.All(result, x => Assert.True(x.Passed, x.Error));
    }
    [Fact]
    public void Finds_sensitive_fields_recursively()
    {
        var result = _engine.Evaluate([new AssertionDefinition(AssertionKind.SensitiveFields, AssertionOperator.NotExists)], 200, 1, new Dictionary<string, string[]>(), """{"data":[{"passwordHash":"oops"}]}""");
        Assert.False(result.Single().Passed); Assert.Contains("$.data[0].passwordHash", result.Single().Actual);
    }
}

public sealed class SecretRedactorTests
{
    [Fact]
    public void Redacts_headers_and_query_values()
    {
        var headers = SecretRedactor.Headers(new Dictionary<string, IEnumerable<string>> { ["Authorization"] = ["Bearer secret"], ["Accept"] = ["application/json"] });
        Assert.Equal("[REDACTED]", headers["Authorization"].Single()); Assert.Equal("application/json", headers["Accept"].Single()); Assert.DoesNotContain("abc", SecretRedactor.Url("https://example.test?token=abc"));
    }

    [Fact]
    public void Redacts_nested_json_response_values()
    {
        var body = SecretRedactor.Body("""{"user":{"accessToken":"abc","name":"Ada"},"password":"bad"}""");
        Assert.DoesNotContain("abc", body); Assert.DoesNotContain("bad", body); Assert.Contains("Ada", body);
    }
}

public sealed class ExecutionEngineTests
{
    [Fact]
    public async Task Executes_asserts_and_extracts_with_shared_pipeline()
    {
        using var client = new HttpClient(new Handler()); var options = new ExecutionOptions(true); var engine = new RequestExecutionEngine(client, new OutboundRequestPolicy(options), new AssertionEngine(), options);
        var request = new RequestDefinition("Login", "POST", "https://example.test/login", Body: new RequestBody(RequestBodyKind.Json, """{"email":"{{email}}"}"""), Assertions: [new AssertionDefinition(AssertionKind.Status, AssertionOperator.Equals, Expected: "200")], Extractions: [new ExtractionRule("token", "json", "accessToken")]);
        var result = await engine.ExecuteAsync(request, new Dictionary<string, string> { ["email"] = "dev@example.test" });
        Assert.True(result.Passed); Assert.Equal("safe-token", result.ExtractedVariables["token"]);
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("{\"email\":\"dev@example.test\"}", await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"accessToken":"safe-token"}""", Encoding.UTF8, "application/json") };
        }
    }
}

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Registration_normalizes_email_hashes_password_and_rejects_duplicates()
    {
        var repository = new UserRepositoryFake();
        var tokens = new TokenService(Options.Create(new JwtOptions { SigningKey = "unit-test-signing-key-that-is-at-least-thirty-two-bytes" }));
        var service = new AuthService(repository, tokens);

        var result = await service.RegisterAsync(new RegisterRequestDto("  ADA@Example.TEST ", "correct horse battery staple", " Ada "), default);

        Assert.Equal("ada@example.test", result.User.Email);
        Assert.Equal("Ada", result.User.DisplayName);
        Assert.NotEqual("correct horse battery staple", repository.User!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("correct horse battery staple", repository.User.PasswordHash));
        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(new RegisterRequestDto("ADA@example.test", "correct horse battery staple", "Ada"), default));
    }

    private sealed class UserRepositoryFake : IUserRepository
    {
        public User? User { get; private set; }
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(User?.Email == email ? User : null);
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(User?.Id == id ? User : null);
        public Task<RefreshToken?> FindRefreshTokenAsync(string hash, CancellationToken ct) => Task.FromResult(User?.RefreshTokens.SingleOrDefault(x => x.TokenHash == hash));
        public Task AddAsync(User user, CancellationToken ct) { User = user; return Task.CompletedTask; }
        public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct) { User?.RefreshTokens.Add(token); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
