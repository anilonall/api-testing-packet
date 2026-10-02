using System.Text.Json;
using System.Xml.Linq;
using ApiTestingPacket.Core;
using ApiTestingPacket.Service.Execution;

namespace ApiTestingPacket.Cli;

public static class CliProgram
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (args.Length < 2 || !args[0].Equals("run", StringComparison.OrdinalIgnoreCase)) { await output.WriteLineAsync("Usage: api-testing-packet run <file.atp.json> [--environment <name>] [--report json|junit] [--output <path>] [--allow-private]"); return 2; }
        try
        {
            var packet = JsonSerializer.Deserialize<PacketFile>(await File.ReadAllTextAsync(args[1], cancellationToken), JsonOptions()) ?? throw new InvalidDataException("Packet is empty.");
            if (packet.SchemaVersion != 1) throw new InvalidDataException($"Unsupported schema version {packet.SchemaVersion}.");
            var environmentName = Option(args, "--environment");
            var environment = environmentName is null ? null : packet.Environments?.SingleOrDefault(x => x.Name.Equals(environmentName, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException($"Environment '{environmentName}' was not found.");
            var options = new ExecutionOptions(args.Contains("--allow-private", StringComparer.OrdinalIgnoreCase));
            using var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, AutomaticDecompression = System.Net.DecompressionMethods.All, UseCookies = false });
            var engine = new RequestExecutionEngine(client, new OutboundRequestPolicy(options), new AssertionEngine(), options); var results = new List<ScenarioResult>();
            if (packet.Scenarios is { Count: > 0 }) foreach (var scenario in packet.Scenarios) results.Add(await engine.ExecuteScenarioAsync(scenario, environment?.Variables, cancellationToken));
            else
            {
                var steps = await ExecuteRequests(packet.Requests, engine, environment?.Variables, cancellationToken);
                results.Add(new ScenarioResult(packet.Name, steps, steps.Sum(x => x.DurationMs)));
            }
            var safeResults = results.Select(Sanitize).ToArray();
            var report = Option(args, "--report")?.ToLowerInvariant() ?? "console";
            var rendered = report switch { "json" => JsonSerializer.Serialize(safeResults, JsonOptions(true)), "junit" => JUnit(safeResults).ToString(), "console" => ConsoleReport(safeResults), _ => throw new InvalidDataException("Report must be console, json, or junit.") };
            var path = Option(args, "--output"); if (path is null) await output.WriteLineAsync(rendered); else await File.WriteAllTextAsync(path, rendered, cancellationToken);
            return results.All(x => x.Passed) ? 0 : 1;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException) { await output.WriteLineAsync($"Error: {ex.Message}"); return 2; }
    }

    private static async Task<IReadOnlyList<ExecutionResult>> ExecuteRequests(IReadOnlyList<RequestDefinition> requests, RequestExecutionEngine engine, IReadOnlyDictionary<string, string>? environment, CancellationToken ct)
    {
        var runtime = VariableResolver.Merge(environment); var results = new List<ExecutionResult>(); foreach (var request in requests) { var result = await engine.ExecuteAsync(request, runtime, ct); results.Add(result); foreach (var pair in result.ExtractedVariables) runtime[pair.Key] = pair.Value; }
        return results;
    }

    private static string ConsoleReport(IEnumerable<ScenarioResult> scenarios)
    {
        var writer = new StringWriter();
        foreach (var scenario in scenarios)
        {
            writer.WriteLine(scenario.Name);
            foreach (var step in scenario.Steps)
            {
                writer.WriteLine($"{(step.Passed ? "PASS" : "FAIL")} {step.RequestName}");
                writer.WriteLine($"  Status: {(step.Status.HasValue ? ((int)step.Status).ToString() : "-")}");
                writer.WriteLine($"  Duration: {step.DurationMs}ms");
                writer.WriteLine($"  Assertions: {step.Assertions.Count(x => x.Passed)}/{step.Assertions.Count} passed");
                if (step.Error is not null) writer.WriteLine($"  FAIL {step.Error}");
                foreach (var failure in step.Assertions.Where(x => !x.Passed))
                {
                    writer.WriteLine($"  FAIL {failure.Name}");
                    writer.WriteLine($"    Expected: {failure.Expected ?? "-"}");
                    writer.WriteLine($"    Actual: {failure.Actual ?? failure.Error ?? "-"}");
                }
            }
        }
        return writer.ToString().TrimEnd();
    }

    private static XDocument JUnit(IEnumerable<ScenarioResult> scenarios)
    {
        var list = scenarios.ToArray();
        var root = new XElement("testsuites",
            new XAttribute("tests", list.Sum(x => x.Steps.Count)),
            new XAttribute("failures", list.Sum(x => x.Steps.Count(y => !y.Passed))));
        foreach (var scenario in list)
        {
            var suite = new XElement("testsuite", new XAttribute("name", scenario.Name), new XAttribute("time", scenario.DurationMs / 1000d));
            foreach (var step in scenario.Steps)
            {
                var test = new XElement("testcase", new XAttribute("name", step.RequestName), new XAttribute("time", step.DurationMs / 1000d));
                if (!step.Passed)
                    test.Add(new XElement("failure", new XAttribute("message", step.Error ?? "Assertions failed"), string.Join(Environment.NewLine, step.Assertions.Where(x => !x.Passed).Select(x => $"{x.Name}: expected {x.Expected}, actual {x.Actual}"))));
                suite.Add(test);
            }
            root.Add(suite);
        }
        return new XDocument(root);
    }

    private static string? Option(string[] args, string name) { var index = Array.FindIndex(args, x => x.Equals(name, StringComparison.OrdinalIgnoreCase)); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
    private static ScenarioResult Sanitize(ScenarioResult scenario) => scenario with { Steps = scenario.Steps.Select(step => step with { Url = SecretRedactor.Url(step.Url), Body = SecretRedactor.Body(step.Body), ResponseHeaders = SecretRedactor.Headers(step.ResponseHeaders.Select(x => KeyValuePair.Create(x.Key, (IEnumerable<string>)x.Value))), ExtractedVariables = new Dictionary<string, string>(), Assertions = step.Assertions.Select(assertion => IsSensitiveAssertion(assertion.Name) ? assertion with { Expected = "[REDACTED]", Actual = "[REDACTED]" } : assertion).ToArray() }).ToArray() };
    private static bool IsSensitiveAssertion(string name) => new[] { "token", "password", "secret", "authorization", "api key", "apikey", "private key" }.Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase));
    private static JsonSerializerOptions JsonOptions(bool indented = false) => new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, WriteIndented = indented };
}
