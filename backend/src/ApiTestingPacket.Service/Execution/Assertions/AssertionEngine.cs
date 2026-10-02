using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using ApiTestingPacket.Core;

namespace ApiTestingPacket.Service.Execution;

public sealed class AssertionEngine
{
    private static readonly HashSet<string> DangerousFields = new(StringComparer.OrdinalIgnoreCase) { "password", "passwordHash", "secret", "privateKey" };

    public IReadOnlyList<AssertionOutcome> Evaluate(IEnumerable<AssertionDefinition> definitions, int status, long durationMs, IReadOnlyDictionary<string, string[]> headers, string? body)
    {
        JsonDocument? document = null;
        if (!string.IsNullOrWhiteSpace(body)) try { document = JsonDocument.Parse(body); } catch (JsonException) { }
        using (document)
            return definitions.Where(x => x.Enabled).Select(x => EvaluateOne(x, status, durationMs, headers, document?.RootElement)).ToArray();
    }

    private static AssertionOutcome EvaluateOne(AssertionDefinition definition, int status, long duration, IReadOnlyDictionary<string, string[]> headers, JsonElement? root)
    {
        var name = definition.Name ?? $"{definition.Kind} {definition.Path} {definition.Operator} {definition.Expected}".Trim();
        try
        {
            var (passed, actual) = definition.Kind switch
            {
                AssertionKind.Status => Compare(status.ToString(CultureInfo.InvariantCulture), definition),
                AssertionKind.Duration => Compare(duration.ToString(CultureInfo.InvariantCulture), definition with { Expected = ParseDuration(definition.Expected).ToString(CultureInfo.InvariantCulture) }),
                AssertionKind.Header => Header(headers, definition),
                AssertionKind.JsonValue => JsonValue(root, definition),
                AssertionKind.JsonType => JsonType(root, definition),
                AssertionKind.ArrayLength => ArrayLength(root, definition),
                AssertionKind.Format => Format(root, definition),
                AssertionKind.RequiredFields => Required(root, definition),
                AssertionKind.SensitiveFields => Sensitive(root),
                _ => (false, "unsupported assertion")
            };
            return new(name, passed, definition.Expected, actual, passed ? null : "Assertion failed");
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or JsonException)
        {
            return new(name, false, definition.Expected, null, ex.Message);
        }
    }

    private static (bool, string) Header(IReadOnlyDictionary<string, string[]> headers, AssertionDefinition d)
    {
        var pair = headers.FirstOrDefault(x => string.Equals(x.Key, d.Path, StringComparison.OrdinalIgnoreCase));
        var exists = pair.Key is not null;
        if (d.Operator == AssertionOperator.Exists) return (exists, exists ? string.Join(", ", pair.Value) : "missing");
        if (d.Operator == AssertionOperator.NotExists) return (!exists, exists ? string.Join(", ", pair.Value) : "missing");
        return Compare(exists ? string.Join(", ", pair.Value) : string.Empty, d);
    }

    private static (bool, string) JsonValue(JsonElement? root, AssertionDefinition d)
    {
        JsonElement value = default;
        var exists = root.HasValue && JsonPath.TryResolve(root.Value, d.Path, out value);
        if (d.Operator == AssertionOperator.Exists) return (exists, exists ? JsonPath.Display(value) : "missing");
        if (d.Operator == AssertionOperator.NotExists) return (!exists, exists ? JsonPath.Display(value) : "missing");
        return exists ? Compare(JsonPath.Display(value), d) : (false, "missing");
    }

    private static (bool, string) JsonType(JsonElement? root, AssertionDefinition d)
    {
        if (!root.HasValue || !JsonPath.TryResolve(root.Value, d.Path, out var value)) return (false, "missing");
        var type = value.ValueKind switch { JsonValueKind.String => "string", JsonValueKind.Number when value.TryGetInt64(out _) => "integer", JsonValueKind.Number => "number", JsonValueKind.True or JsonValueKind.False => "boolean", JsonValueKind.Object => "object", JsonValueKind.Array => "array", JsonValueKind.Null => "null", _ => "undefined" };
        return (string.Equals(type, d.Expected, StringComparison.OrdinalIgnoreCase) || d.Expected == "number" && type == "integer", type);
    }

    private static (bool, string) ArrayLength(JsonElement? root, AssertionDefinition d)
    {
        if (!root.HasValue || !JsonPath.TryResolve(root.Value, d.Path, out var value) || value.ValueKind != JsonValueKind.Array) return (false, "not an array");
        return Compare(value.GetArrayLength().ToString(CultureInfo.InvariantCulture), d);
    }

    private static (bool, string) Format(JsonElement? root, AssertionDefinition d)
    {
        if (!root.HasValue || !JsonPath.TryResolve(root.Value, d.Path, out var value) || value.ValueKind != JsonValueKind.String) return (false, "missing or not a string");
        var text = value.GetString()!;
        var valid = d.Expected?.ToLowerInvariant() switch
        {
            "email" => MailAddress.TryCreate(text, out _),
            "uuid" => Guid.TryParse(text, out _),
            "url" => Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"),
            "date" => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "datetime" => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
            _ => false
        };
        return (valid, text);
    }

    private static (bool, string) Required(JsonElement? root, AssertionDefinition d)
    {
        if (!root.HasValue) return (false, "not JSON");
        var missing = (d.Expected ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(path => !JsonPath.TryResolve(root.Value, path, out _)).ToArray();
        return (missing.Length == 0, missing.Length == 0 ? "all present" : $"missing: {string.Join(", ", missing)}");
    }

    private static (bool, string) Sensitive(JsonElement? root)
    {
        if (!root.HasValue) return (true, "not JSON");
        var found = new List<string>();
        Scan(root.Value, "$", found);
        return (found.Count == 0, found.Count == 0 ? "none found" : string.Join(", ", found));
    }

    private static void Scan(JsonElement element, string path, List<string> found)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) { var child = $"{path}.{property.Name}"; if (DangerousFields.Contains(property.Name)) found.Add(child); Scan(property.Value, child, found); }
        else if (element.ValueKind == JsonValueKind.Array)
        { var index = 0; foreach (var child in element.EnumerateArray()) Scan(child, $"{path}[{index++}]", found); }
    }

    private static (bool, string) Compare(string actual, AssertionDefinition d)
    {
        var expected = d.Expected ?? string.Empty;
        var result = d.Operator switch
        {
            AssertionOperator.Equals => string.Equals(actual, expected, StringComparison.Ordinal),
            AssertionOperator.NotEquals => !string.Equals(actual, expected, StringComparison.Ordinal),
            AssertionOperator.Contains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            AssertionOperator.In => expected.Split(',', StringSplitOptions.TrimEntries).Contains(actual, StringComparer.Ordinal),
            AssertionOperator.GreaterThan => Number(actual) > Number(expected),
            AssertionOperator.LessThan => Number(actual) < Number(expected),
            AssertionOperator.GreaterThanOrEqual => Number(actual) >= Number(expected),
            AssertionOperator.LessThanOrEqual => Number(actual) <= Number(expected),
            _ => false
        };
        return (result, actual);
    }

    private static decimal Number(string value) => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static long ParseDuration(string? value)
    {
        var text = (value ?? "0").Trim().ToLowerInvariant();
        if (text.EndsWith("ms")) return (long)decimal.Parse(text[..^2], CultureInfo.InvariantCulture);
        if (text.EndsWith('s')) return (long)(decimal.Parse(text[..^1], CultureInfo.InvariantCulture) * 1000);
        return long.Parse(text, CultureInfo.InvariantCulture);
    }
}
