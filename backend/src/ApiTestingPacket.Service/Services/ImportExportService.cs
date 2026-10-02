using System.Text.Json;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;

namespace ApiTestingPacket.Service.Services;

public sealed class ImportExportService(
    ICollectionRepository collections,
    IScenarioRepository scenarios,
    IEnvironmentRepository environments) : IImportExportService
{
    public async Task<(byte[] Content, string FileName)> ExportCollectionAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var collection = await collections.GetAsync(userId, id, true, ct) ?? throw new NotFoundException();
        var requests = collection.Requests.Select(x => JsonSerializer.Deserialize<RequestDefinition>(x.DefinitionJson, JsonDefaults.Web)!).ToArray();
        var scenarioValues = await scenarios.ListAsync(userId, ct);
        var definitions = scenarioValues.Select(x => JsonSerializer.Deserialize<ScenarioDefinition>(x.DefinitionJson, JsonDefaults.Web)!).ToArray();
        var packet = new PacketFile(1, collection.Name, requests, definitions);
        return (JsonSerializer.SerializeToUtf8Bytes(packet, new JsonSerializerOptions(JsonDefaults.Web) { WriteIndented = true }), $"{Slug(collection.Name)}.atp.json");
    }

    public async Task<ImportResultDto> ImportNativeAsync(Guid userId, NativeImportRequestDto input, CancellationToken ct)
    {
        var errors = PacketValidator.Validate(input.Packet);
        if (errors.Length > 0) throw new RequestValidationException(new Dictionary<string, string[]> { ["packet"] = errors });
        var collection = new Collection { UserId = userId, Name = input.Packet.Name };
        foreach (var request in input.Packet.Requests) collection.Requests.Add(Request(userId, request));
        await collections.AddAsync(collection, ct);
        foreach (var definition in input.Packet.Scenarios ?? []) await scenarios.AddAsync(new Scenario { UserId = userId, Name = definition.Name, DefinitionJson = JsonSerializer.Serialize(definition, JsonDefaults.Web) }, ct);
        if (input.IncludeEnvironments)
            foreach (var environment in input.Packet.Environments ?? [])
                await environments.AddAsync(new ApiEnvironment { UserId = userId, Name = environment.Name, VariablesJson = JsonSerializer.Serialize(environment.Variables.Select(x => new EnvironmentVariableDto(x.Key, environment.Secrets?.Contains(x.Key) == true ? "" : x.Value, environment.Secrets?.Contains(x.Key) == true)), JsonDefaults.Web) }, ct);
        await collections.SaveChangesAsync(ct);
        return new(collection.Id, collection.Requests.Count, []);
    }

    public async Task<ImportResultDto> ImportOpenApiAsync(Guid userId, OpenApiImportRequestDto input, CancellationToken ct)
    {
        try
        {
            var imported = OpenApiImporter.Parse(input.Document, input.Format, input.CollectionName);
            var collection = new Collection { UserId = userId, Name = imported.Title };
            foreach (var request in imported.Requests) collection.Requests.Add(Request(userId, request));
            await collections.AddAsync(collection, ct);
            await collections.SaveChangesAsync(ct);
            return new(collection.Id, collection.Requests.Count, imported.Warnings);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or YamlDotNet.Core.YamlException)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["document"] = [exception.Message] });
        }
    }

    private static ApiRequest Request(Guid userId, RequestDefinition definition) => new() { UserId = userId, Name = definition.Name, Method = definition.Method, Url = definition.Url, DefinitionJson = JsonSerializer.Serialize(definition, JsonDefaults.Web) };
    private static string Slug(string value) => string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')).Trim('-');
}

internal static class PacketValidator
{
    public static string[] Validate(PacketFile packet)
    {
        var errors = new List<string>();
        if (packet.SchemaVersion != 1) errors.Add("Only schema version 1 is supported.");
        if (string.IsNullOrWhiteSpace(packet.Name)) errors.Add("Packet name is required.");
        if (packet.Requests.Count > 1000) errors.Add("A packet can contain at most 1000 requests.");
        if ((packet.Scenarios?.Sum(x => x.Steps.Count) ?? 0) > 1000) errors.Add("A packet can contain at most 1000 scenario steps.");
        var methods = new HashSet<string>(["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"], StringComparer.OrdinalIgnoreCase);
        var scenarioRequests = packet.Scenarios?.SelectMany(x => x.Steps).Select(x => x.Request) ?? [];
        foreach (var request in packet.Requests.Concat(scenarioRequests))
        {
            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Every request needs a name.");
            if (!methods.Contains(request.Method)) errors.Add($"Request '{request.Name}' uses an unsupported HTTP method.");
            if (string.IsNullOrWhiteSpace(request.Url)) errors.Add($"Request '{request.Name}' needs a URL template.");
            if (request.TimeoutMs is < 100 or > 300_000) errors.Add($"Request '{request.Name}' timeout must be between 100 and 300000 ms.");
            if (request.Body is { Kind: RequestBodyKind.Json, Content: not null })
                try { JsonDocument.Parse(request.Body.Content).Dispose(); }
                catch (JsonException) { errors.Add($"Request '{request.Name}' contains an invalid JSON body."); }
        }
        return errors.Distinct().Take(50).ToArray();
    }
}
