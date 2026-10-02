using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace ApiTestingPacket.Service.Execution;

public static partial class SecretRedactor
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "proxy-authorization", "cookie", "set-cookie", "x-api-key", "api-key", "password", "token", "secret", "privatekey", "client_secret"
    };

    [GeneratedRegex(@"(?i)(token|secret|password|api[-_]?key)=([^&\s]+)")]
    private static partial Regex QuerySecret();

    public static IReadOnlyDictionary<string, string[]> Headers(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) =>
        headers.ToDictionary(x => x.Key, x => IsSensitive(x.Key) ? ["[REDACTED]"] : x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

    public static string Url(string url) => QuerySecret().Replace(url, "$1=[REDACTED]");
    public static bool IsSensitive(string name)
    {
        var normalized = name.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        return SensitiveNames.Contains(name) || SensitiveNames.Contains(normalized) || normalized.EndsWith("token", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("password", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("secret", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("apikey", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("privatekey", StringComparison.OrdinalIgnoreCase);
    }

    public static string? Body(string? body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        try
        {
            var node = JsonNode.Parse(body);
            RedactNode(node);
            return node?.ToJsonString();
        }
        catch (System.Text.Json.JsonException)
        {
            return QuerySecret().Replace(body, "$1=[REDACTED]");
        }
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject obj)
            foreach (var property in obj.ToArray())
                if (IsSensitive(property.Key)) obj[property.Key] = "[REDACTED]"; else RedactNode(property.Value);
        else if (node is JsonArray array)
            foreach (var child in array) RedactNode(child);
    }
}
