using System.Text.Json;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;
using ApiTestingPacket.Service.Execution;

namespace ApiTestingPacket.Service.Services;

public sealed class ScenarioService(IScenarioRepository scenarios, IEnvironmentService environments, IExecutionRepository history, RequestExecutionEngine engine) : IScenarioService
{
    public Task<IReadOnlyList<Scenario>> ListAsync(Guid userId, CancellationToken ct) => scenarios.ListAsync(userId, ct);
    public async Task<ScenarioDto> GetAsync(Guid userId, Guid id, CancellationToken ct) { var value = await scenarios.GetAsync(userId, id, ct) ?? throw new NotFoundException(); return new(value.Id, value.Name, value.DefinitionJson, value.UpdatedAt); }
    public async Task<Scenario> CreateAsync(Guid userId, ScenarioInputDto input, CancellationToken ct) { var value = new Scenario { UserId = userId, Name = input.Definition.Name, DefinitionJson = JsonSerializer.Serialize(input.Definition, JsonDefaults.Web) }; await scenarios.AddAsync(value, ct); await scenarios.SaveChangesAsync(ct); return value; }
    public async Task<Scenario> UpdateAsync(Guid userId, Guid id, ScenarioInputDto input, CancellationToken ct) { var value = await scenarios.GetAsync(userId, id, ct) ?? throw new NotFoundException(); value.Name = input.Definition.Name; value.DefinitionJson = JsonSerializer.Serialize(input.Definition, JsonDefaults.Web); value.UpdatedAt = DateTimeOffset.UtcNow; await scenarios.SaveChangesAsync(ct); return value; }
    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct) { if (!await scenarios.DeleteAsync(userId, id, ct)) throw new NotFoundException(); }
    public async Task<ScenarioResult> ExecuteAsync(Guid userId, Guid id, ExecuteRequestDto input, CancellationToken ct) { var value = await scenarios.GetAsync(userId, id, ct) ?? throw new NotFoundException(); var definition = JsonSerializer.Deserialize<ScenarioDefinition>(value.DefinitionJson, JsonDefaults.Web) ?? throw new InvalidOperationException("Saved scenario definition is invalid."); var result = await engine.ExecuteScenarioAsync(definition, await environments.ResolveAsync(userId, input, ct), ct); await history.AddRangeAsync(result.Steps.Select(x => ExecutionSnapshots.Create(userId, null, id, x)), ct); await history.SaveChangesAsync(ct); return result; }
}
