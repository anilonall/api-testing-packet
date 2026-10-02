using System.Text.Json;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;
using ApiTestingPacket.Service.Execution;

namespace ApiTestingPacket.Service.Services;

public sealed class RequestService(IRequestRepository requests, ICollectionRepository collections, IEnvironmentService environments, IExecutionRepository history, RequestExecutionEngine engine) : IRequestService
{
    public async Task<Guid> CreateAsync(Guid userId, SavedRequestInputDto input, CancellationToken ct) { await ValidateTarget(userId, input, ct); var value = Map(input, userId); await requests.AddAsync(value, ct); await requests.SaveChangesAsync(ct); return value.Id; }
    public async Task<Guid> UpdateAsync(Guid userId, Guid id, SavedRequestInputDto input, CancellationToken ct) { var value = await requests.GetAsync(userId, id, ct) ?? throw new NotFoundException(); await ValidateTarget(userId, input, ct); Apply(value, input); await requests.SaveChangesAsync(ct); return value.Id; }
    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct) { if (!await requests.DeleteAsync(userId, id, ct)) throw new NotFoundException(); }
    public async Task<Guid> DuplicateAsync(Guid userId, Guid id, CancellationToken ct) { var source = await requests.GetAsync(userId, id, ct) ?? throw new NotFoundException(); var copy = new ApiRequest { UserId = userId, CollectionId = source.CollectionId, FolderId = source.FolderId, Name = source.Name + " copy", Method = source.Method, Url = source.Url, DefinitionJson = source.DefinitionJson, SortOrder = source.SortOrder }; await requests.AddAsync(copy, ct); await requests.SaveChangesAsync(ct); return copy.Id; }
    public async Task<ExecutionResult> ExecuteAsync(Guid userId, Guid id, ExecuteRequestDto input, CancellationToken ct) { var value = await requests.GetAsync(userId, id, ct) ?? throw new NotFoundException(); var definition = JsonSerializer.Deserialize<RequestDefinition>(value.DefinitionJson, JsonDefaults.Web) ?? throw new InvalidOperationException("Saved request definition is invalid."); var result = await engine.ExecuteAsync(definition, await environments.ResolveAsync(userId, input, ct), ct); await history.AddAsync(ExecutionSnapshots.Create(userId, id, null, result), ct); await history.SaveChangesAsync(ct); return result; }
    private static ApiRequest Map(SavedRequestInputDto input, Guid userId) => new() { UserId = userId, CollectionId = input.CollectionId, FolderId = input.FolderId, Name = input.Definition.Name, Method = input.Definition.Method, Url = input.Definition.Url, DefinitionJson = JsonSerializer.Serialize(input.Definition, JsonDefaults.Web), SortOrder = input.SortOrder };
    private static void Apply(ApiRequest value, SavedRequestInputDto input) { value.CollectionId = input.CollectionId; value.FolderId = input.FolderId; value.Name = input.Definition.Name; value.Method = input.Definition.Method; value.Url = input.Definition.Url; value.DefinitionJson = JsonSerializer.Serialize(input.Definition, JsonDefaults.Web); value.SortOrder = input.SortOrder; value.UpdatedAt = DateTimeOffset.UtcNow; }
    private async Task ValidateTarget(Guid userId, SavedRequestInputDto input, CancellationToken ct) { if (!await requests.CollectionExistsAsync(userId, input.CollectionId, ct)) throw new NotFoundException(); if (input.FolderId.HasValue && !await collections.FolderExistsAsync(userId, input.CollectionId, input.FolderId.Value, ct)) throw new InvalidOperationException("Folder is not part of the selected collection."); }
}

internal static class ExecutionSnapshots
{
    public static ApiTestingPacket.Core.Entities.Execution Create(Guid userId, Guid? requestId, Guid? scenarioId, ExecutionResult result) => new() { UserId = userId, RequestId = requestId, ScenarioId = scenarioId, RequestName = result.RequestName, Method = result.Method, SafeUrl = SecretRedactor.Url(result.Url), StatusCode = result.Status.HasValue ? (int)result.Status.Value : null, DurationMs = result.DurationMs, Passed = result.Passed, SnapshotJson = JsonSerializer.Serialize(result with { Body = SecretRedactor.Body(result.Body), ResponseHeaders = SecretRedactor.Headers(result.ResponseHeaders.Select(x => KeyValuePair.Create(x.Key, (IEnumerable<string>)x.Value))), ExtractedVariables = new Dictionary<string, string>() }, JsonDefaults.Web) };
}
