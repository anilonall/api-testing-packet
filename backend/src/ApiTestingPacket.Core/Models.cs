using System.Net;
using System.Text.Json.Serialization;

namespace ApiTestingPacket.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestBodyKind { None, Json, Text, FormUrlEncoded, Multipart }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestAuthKind { None, Bearer, Basic, ApiKey }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiKeyLocation { Header, Query }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssertionKind { Status, Duration, Header, JsonValue, JsonType, ArrayLength, Format, RequiredFields, SensitiveFields }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssertionOperator { Equals, NotEquals, Exists, NotExists, Contains, In, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual }

public sealed record KeyValueItem(string Name, string Value, bool Enabled = true, bool Secret = false);
public sealed record RequestAuth(RequestAuthKind Kind = RequestAuthKind.None, string? Token = null, string? Username = null, string? Password = null, string? ApiKeyName = null, string? ApiKeyValue = null, ApiKeyLocation ApiKeyLocation = ApiKeyLocation.Header);
public sealed record RequestBody(RequestBodyKind Kind = RequestBodyKind.None, string? Content = null, IReadOnlyList<KeyValueItem>? Fields = null);
public sealed record AssertionDefinition(AssertionKind Kind, AssertionOperator Operator, string? Path = null, string? Expected = null, string? Name = null, bool Enabled = true);
public sealed record ExtractionRule(string Variable, string Source, string Path, bool Secret = false);

public sealed record RequestDefinition(
    string Name,
    string Method,
    string Url,
    IReadOnlyList<KeyValueItem>? Query = null,
    IReadOnlyList<KeyValueItem>? Headers = null,
    RequestAuth? Auth = null,
    RequestBody? Body = null,
    IReadOnlyList<AssertionDefinition>? Assertions = null,
    IReadOnlyList<ExtractionRule>? Extractions = null,
    int TimeoutMs = 30_000);

public sealed record AssertionOutcome(string Name, bool Passed, string? Expected = null, string? Actual = null, string? Error = null);
public sealed record ExecutionResult(
    string RequestName,
    string Method,
    string Url,
    HttpStatusCode? Status,
    long DurationMs,
    long ResponseSize,
    IReadOnlyDictionary<string, string[]> ResponseHeaders,
    string? Body,
    string? ContentType,
    IReadOnlyList<AssertionOutcome> Assertions,
    IReadOnlyDictionary<string, string> ExtractedVariables,
    string? Error = null,
    bool Cancelled = false)
{
    public bool Passed => Error is null && Assertions.All(x => x.Passed);
}

public sealed record ScenarioDefinition(string Name, IReadOnlyList<ScenarioStepDefinition> Steps, bool StopOnFailure = true);
public sealed record ScenarioStepDefinition(string Name, RequestDefinition Request);
public sealed record ScenarioResult(string Name, IReadOnlyList<ExecutionResult> Steps, long DurationMs, bool Cancelled = false)
{
    public bool Passed => !Cancelled && Steps.Count > 0 && Steps.All(x => x.Passed);
}

public sealed record PacketEnvironment(string Name, IReadOnlyDictionary<string, string> Variables, IReadOnlyList<string>? Secrets = null);
public sealed record PacketFile(int SchemaVersion, string Name, IReadOnlyList<RequestDefinition> Requests, IReadOnlyList<ScenarioDefinition>? Scenarios = null, IReadOnlyList<PacketEnvironment>? Environments = null);
