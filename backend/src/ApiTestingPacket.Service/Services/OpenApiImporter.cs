using System.Text.Json;
using ApiTestingPacket.Core;
using YamlDotNet.Serialization;

namespace ApiTestingPacket.Service.Services;

public sealed record OpenApiImportResult(string Title, IReadOnlyList<RequestDefinition> Requests, IReadOnlyList<string> Warnings);

public static class OpenApiImporter
{
    private static readonly HashSet<string> Methods = new(StringComparer.OrdinalIgnoreCase) { "get", "post", "put", "patch", "delete", "head", "options" };

    public static OpenApiImportResult Parse(string document, string format, string? requestedName)
    {
        if (document.Length > 5_000_000) throw new InvalidDataException("OpenAPI document exceeds the 5 MB import limit.");
        var json = format.Equals("yaml", StringComparison.OrdinalIgnoreCase) || format.Equals("yml", StringComparison.OrdinalIgnoreCase) ? YamlToJson(document) : document;
        using var root = JsonDocument.Parse(json);
        var version = root.RootElement.TryGetProperty("openapi", out var openapi) ? openapi.GetString() : null;
        if (version is null || !version.StartsWith("3.", StringComparison.Ordinal)) throw new InvalidDataException("Only OpenAPI 3.x documents are supported.");
        var title = requestedName ?? (root.RootElement.TryGetProperty("info", out var info) && info.TryGetProperty("title", out var titleNode) ? titleNode.GetString() : null) ?? "Imported API";
        var server = root.RootElement.TryGetProperty("servers", out var servers) && servers.ValueKind == JsonValueKind.Array && servers.GetArrayLength() > 0 && servers[0].TryGetProperty("url", out var serverUrl) ? serverUrl.GetString() : null;
        var baseUrl = string.IsNullOrWhiteSpace(server) ? "{{baseUrl}}" : server.TrimEnd('/');
        var requests = new List<RequestDefinition>(); var warnings = new List<string>();
        if (!root.RootElement.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object) throw new InvalidDataException("The OpenAPI document has no paths object.");
        foreach (var path in paths.EnumerateObject())
            foreach (var operation in path.Value.EnumerateObject().Where(x => Methods.Contains(x.Name)))
            {
                var op = operation.Value; var name = op.TryGetProperty("summary", out var summary) ? summary.GetString() : null; name ??= op.TryGetProperty("operationId", out var operationId) ? operationId.GetString() : null; name ??= $"{operation.Name.ToUpperInvariant()} {path.Name}";
                var query = new List<KeyValueItem>(); var headers = new List<KeyValueItem>();
                foreach (var source in new[] { path.Value, op }) if (source.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Array)
                    foreach (var parameter in parameters.EnumerateArray()) { if (!parameter.TryGetProperty("name", out var pn) || !parameter.TryGetProperty("in", out var pin)) continue; var item = new KeyValueItem(pn.GetString() ?? "parameter", Example(parameter), false); if (pin.GetString() == "query") query.Add(item); else if (pin.GetString() == "header") headers.Add(item); else if (pin.GetString() != "path") warnings.Add($"Parameter location '{pin.GetString()}' on {operation.Name.ToUpperInvariant()} {path.Name} was not imported."); }
                var body = Body(op, warnings, operation.Name, path.Name);
                requests.Add(new RequestDefinition(name, operation.Name.ToUpperInvariant(), baseUrl + path.Name, query, headers, Body: body));
            }
        if (requests.Count == 0) throw new InvalidDataException("No supported HTTP operations were found.");
        if (root.RootElement.TryGetProperty("components", out var components) && components.TryGetProperty("securitySchemes", out _)) warnings.Add("Security schemes were detected. Review imported request authentication; complex OAuth flows are not imported automatically.");
        return new(title, requests, warnings.Distinct().ToArray());
    }

    private static RequestBody? Body(JsonElement operation, List<string> warnings, string method, string path)
    {
        if (!operation.TryGetProperty("requestBody", out var requestBody) || !requestBody.TryGetProperty("content", out var content)) return null;
        foreach (var media in content.EnumerateObject())
        {
            if (media.Name != "application/json") continue;
            if (media.Value.TryGetProperty("example", out var example)) return new RequestBody(RequestBodyKind.Json, JsonSerializer.Serialize(example));
            if (media.Value.TryGetProperty("schema", out var schema) && schema.TryGetProperty("example", out example)) return new RequestBody(RequestBodyKind.Json, JsonSerializer.Serialize(example));
            return new RequestBody(RequestBodyKind.Json, "{}");
        }
        warnings.Add($"Non-JSON request body on {method.ToUpperInvariant()} {path} was not imported."); return null;
    }

    private static string Example(JsonElement parameter)
    {
        if (parameter.TryGetProperty("example", out var example)) return example.ValueKind == JsonValueKind.String ? example.GetString() ?? "" : example.GetRawText();
        return "";
    }

    private static string YamlToJson(string yaml)
    {
        var value = new DeserializerBuilder().Build().Deserialize<object>(yaml);
        return JsonSerializer.Serialize(value);
    }
}
