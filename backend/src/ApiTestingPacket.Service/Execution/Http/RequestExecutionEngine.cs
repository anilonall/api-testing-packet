using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiTestingPacket.Core;

namespace ApiTestingPacket.Service.Execution;

public sealed record ExecutionOptions(bool AllowPrivateNetworks = false, int MaxResponseBytes = 5 * 1024 * 1024, int MaxRedirects = 5);

public interface IOutboundRequestPolicy
{
    Task ValidateAsync(Uri uri, CancellationToken cancellationToken);
}

public sealed class OutboundRequestPolicy(ExecutionOptions options) : IOutboundRequestPolicy
{
    public async Task ValidateAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Only HTTP and HTTPS URLs are allowed.");
        if (options.AllowPrivateNetworks) return;
        if (uri.IsLoopback) throw new InvalidOperationException("Loopback destinations are disabled by outbound policy.");
        IPAddress[] addresses;
        try { addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken); }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException) { throw new InvalidOperationException("The destination host could not be resolved.", ex); }
        if (addresses.Length == 0 || addresses.Any(IsPrivate)) throw new InvalidOperationException("Private, local, or unspecified destinations are disabled by outbound policy.");
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return true;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
        var b = address.GetAddressBytes();
        return b[0] == 10 || b[0] == 127 || b[0] == 0 || b[0] == 169 && b[1] == 254 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168;
    }
}

public sealed class RequestExecutionEngine(HttpClient client, IOutboundRequestPolicy policy, AssertionEngine assertions, ExecutionOptions options)
{
    public async Task<ExecutionResult> ExecuteAsync(RequestDefinition definition, IReadOnlyDictionary<string, string>? variables = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var safeUrl = SecretRedactor.Url(definition.Url);
        try
        {
            var values = variables ?? new Dictionary<string, string>();
            var url = BuildUrl(VariableResolver.Resolve(definition.Url, values), definition.Query, definition.Auth, values);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) throw new InvalidOperationException("The resolved request URL is invalid.");
            await policy.ValidateAsync(uri, cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Math.Clamp(definition.TimeoutMs, 100, 300_000));
            using var request = BuildRequest(definition, uri, values);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            stopwatch.Stop();
            var body = await ReadBoundedAsync(response.Content, options.MaxResponseBytes, timeout.Token);
            var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
            var outcomes = assertions.Evaluate(definition.Assertions ?? [], (int)response.StatusCode, stopwatch.ElapsedMilliseconds, headers, body);
            var extracted = Extract(definition.Extractions ?? [], headers, body);
            return new(definition.Name, definition.Method.ToUpperInvariant(), SecretRedactor.Url(uri.ToString()), response.StatusCode, stopwatch.ElapsedMilliseconds, Encoding.UTF8.GetByteCount(body), headers, body, response.Content.Headers.ContentType?.MediaType, outcomes, extracted);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return Failure(definition, safeUrl, stopwatch.ElapsedMilliseconds, "Execution was cancelled.", true);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return Failure(definition, safeUrl, stopwatch.ElapsedMilliseconds, "Request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or VariableResolutionException or UriFormatException)
        {
            stopwatch.Stop();
            return Failure(definition, safeUrl, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<ScenarioResult> ExecuteScenarioAsync(ScenarioDefinition scenario, IReadOnlyDictionary<string, string>? environment = null, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var runtime = VariableResolver.Merge(environment);
        var results = new List<ExecutionResult>();
        foreach (var step in scenario.Steps)
        {
            var result = await ExecuteAsync(step.Request with { Name = step.Name }, runtime, cancellationToken);
            results.Add(result);
            foreach (var pair in result.ExtractedVariables) runtime[pair.Key] = pair.Value;
            if (scenario.StopOnFailure && !result.Passed) break;
            if (result.Cancelled) break;
        }
        watch.Stop();
        return new(scenario.Name, results, watch.ElapsedMilliseconds, results.LastOrDefault()?.Cancelled == true);
    }

    private static HttpRequestMessage BuildRequest(RequestDefinition d, Uri uri, IReadOnlyDictionary<string, string> variables)
    {
        var request = new HttpRequestMessage(new HttpMethod(d.Method.ToUpperInvariant()), uri);
        request.Content = BuildContent(d.Body, variables);
        foreach (var header in d.Headers ?? [])
            if (header.Enabled && !request.Headers.TryAddWithoutValidation(header.Name, VariableResolver.Resolve(header.Value, variables)))
            {
                request.Content ??= new ByteArrayContent([]);
                request.Content.Headers.TryAddWithoutValidation(header.Name, VariableResolver.Resolve(header.Value, variables));
            }
        ApplyAuth(request, d.Auth, variables);
        return request;
    }

    private static void ApplyAuth(HttpRequestMessage request, RequestAuth? auth, IReadOnlyDictionary<string, string> variables)
    {
        if (auth is null) return;
        switch (auth.Kind)
        {
            case RequestAuthKind.Bearer:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", VariableResolver.Resolve(auth.Token ?? string.Empty, variables)); break;
            case RequestAuthKind.Basic:
                var raw = $"{VariableResolver.Resolve(auth.Username ?? string.Empty, variables)}:{VariableResolver.Resolve(auth.Password ?? string.Empty, variables)}";
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))); break;
            case RequestAuthKind.ApiKey when auth.ApiKeyLocation == ApiKeyLocation.Header:
                if (!string.IsNullOrWhiteSpace(auth.ApiKeyName)) request.Headers.TryAddWithoutValidation(auth.ApiKeyName, VariableResolver.Resolve(auth.ApiKeyValue ?? string.Empty, variables)); break;
        }
    }

    private static HttpContent? BuildContent(RequestBody? body, IReadOnlyDictionary<string, string> variables)
    {
        if (body is null || body.Kind == RequestBodyKind.None) return null;
        var content = VariableResolver.Resolve(body.Content ?? string.Empty, variables);
        return body.Kind switch
        {
            RequestBodyKind.Json => new StringContent(content, Encoding.UTF8, "application/json"),
            RequestBodyKind.Text => new StringContent(content, Encoding.UTF8, "text/plain"),
            RequestBodyKind.FormUrlEncoded => new FormUrlEncodedContent((body.Fields ?? []).Where(x => x.Enabled).Select(x => new KeyValuePair<string, string>(x.Name, VariableResolver.Resolve(x.Value, variables)))),
            RequestBodyKind.Multipart => Multipart(body.Fields ?? [], variables),
            _ => null
        };
    }

    private static HttpContent Multipart(IEnumerable<KeyValueItem> fields, IReadOnlyDictionary<string, string> variables)
    {
        var content = new MultipartFormDataContent();
        foreach (var field in fields.Where(x => x.Enabled)) content.Add(new StringContent(VariableResolver.Resolve(field.Value, variables)), field.Name);
        return content;
    }

    private static string BuildUrl(string url, IReadOnlyList<KeyValueItem>? query, RequestAuth? auth, IReadOnlyDictionary<string, string> variables)
    {
        var parts = new List<string>();
        foreach (var item in query ?? []) if (item.Enabled) parts.Add($"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(VariableResolver.Resolve(item.Value, variables))}");
        if (auth is { Kind: RequestAuthKind.ApiKey, ApiKeyLocation: ApiKeyLocation.Query } && !string.IsNullOrWhiteSpace(auth.ApiKeyName))
            parts.Add($"{Uri.EscapeDataString(auth.ApiKeyName)}={Uri.EscapeDataString(VariableResolver.Resolve(auth.ApiKeyValue ?? string.Empty, variables))}");
        return parts.Count == 0 ? url : $"{url}{(url.Contains('?', StringComparison.Ordinal) ? '&' : '?')}{string.Join('&', parts)}";
    }

    private static async Task<string> ReadBoundedAsync(HttpContent content, int maxBytes, CancellationToken token)
    {
        await using var stream = await content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, token);
            if (read == 0) break;
            if (buffer.Length + read > maxBytes) throw new InvalidOperationException($"Response exceeded the {maxBytes} byte limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), token);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static IReadOnlyDictionary<string, string> Extract(IEnumerable<ExtractionRule> rules, IReadOnlyDictionary<string, string[]> headers, string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        JsonDocument? json = null;
        foreach (var rule in rules)
        {
            if (rule.Source.Equals("header", StringComparison.OrdinalIgnoreCase))
            {
                var pair = headers.FirstOrDefault(x => string.Equals(x.Key, rule.Path, StringComparison.OrdinalIgnoreCase));
                if (pair.Key is not null) result[rule.Variable] = pair.Value.FirstOrDefault() ?? string.Empty;
            }
            else
            {
                try { json ??= JsonDocument.Parse(body); } catch (JsonException) { continue; }
                if (JsonPath.TryResolve(json.RootElement, rule.Path, out var value)) result[rule.Variable] = JsonPath.Display(value);
            }
        }
        json?.Dispose();
        return result;
    }

    private static ExecutionResult Failure(RequestDefinition d, string url, long duration, string error, bool cancelled = false) =>
        new(d.Name, d.Method.ToUpperInvariant(), url, null, duration, 0, new Dictionary<string, string[]>(), null, null, [], new Dictionary<string, string>(), error, cancelled);
}
