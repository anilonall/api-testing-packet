using System.Text.RegularExpressions;

namespace ApiTestingPacket.Service.Execution;

public static partial class VariableResolver
{
    [GeneratedRegex(@"\{\{\s*([A-Za-z_][A-Za-z0-9_.-]*)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();

    public static string Resolve(string template, IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = Placeholder().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (variables.TryGetValue(key, out var value)) return value;
            var found = variables.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (found.Key is not null) return found.Value;
            missing.Add(key);
            return match.Value;
        });
        if (missing.Count > 0) throw new VariableResolutionException(missing);
        return result;
    }

    public static Dictionary<string, string> Merge(params IReadOnlyDictionary<string, string>?[] levels)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var level in levels)
            if (level is not null)
                foreach (var pair in level) result[pair.Key] = pair.Value;
        return result;
    }
}

public sealed class VariableResolutionException(IEnumerable<string> names)
    : Exception($"Missing variables: {string.Join(", ", names.Order(StringComparer.OrdinalIgnoreCase))}")
{
    public IReadOnlyList<string> Names { get; } = names.Order(StringComparer.OrdinalIgnoreCase).ToArray();
}
