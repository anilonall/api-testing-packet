using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiTestingPacket.Service.Execution;

public static partial class JsonPath
{
    [GeneratedRegex(@"(?:^|\.)([^.\[\]]+)|\[(\d+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentPattern();

    public static bool TryResolve(JsonElement root, string? path, out JsonElement value)
    {
        value = root;
        if (string.IsNullOrWhiteSpace(path) || path == "$") return true;
        var normalized = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;
        var matches = SegmentPattern().Matches(normalized);
        if (matches.Count == 0) return false;
        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(match.Groups[1].Value, out value)) return false;
            }
            else
            {
                if (value.ValueKind != JsonValueKind.Array || !int.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var index) || index >= value.GetArrayLength()) return false;
                value = value[index];
            }
        }
        return true;
    }

    public static string Display(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null => "null",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText()
    };
}
