using System.Text.Json;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;

namespace ApiTestingPacket.Service.Services;

public sealed class EnvironmentService(IEnvironmentRepository environments) : IEnvironmentService
{
    public async Task<IReadOnlyList<EnvironmentSummaryDto>> ListAsync(Guid userId, CancellationToken ct) => (await environments.ListAsync(userId, ct)).Select(x => new EnvironmentSummaryDto(x.Id, x.Name, x.UpdatedAt)).ToArray();
    public async Task<ApiEnvironment> CreateAsync(Guid userId, EnvironmentInputDto input, CancellationToken ct) { var value = new ApiEnvironment { UserId = userId, Name = input.Name.Trim(), VariablesJson = JsonSerializer.Serialize(input.Variables, JsonDefaults.Web) }; await environments.AddAsync(value, ct); await environments.SaveChangesAsync(ct); return value; }
    public async Task<ApiEnvironment> UpdateAsync(Guid userId, Guid id, EnvironmentInputDto input, CancellationToken ct) { var value = await environments.GetAsync(userId, id, ct) ?? throw new NotFoundException(); value.Name = input.Name.Trim(); value.VariablesJson = JsonSerializer.Serialize(input.Variables, JsonDefaults.Web); value.UpdatedAt = DateTimeOffset.UtcNow; await environments.SaveChangesAsync(ct); return value; }
    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct) { if (!await environments.DeleteAsync(userId, id, ct)) throw new NotFoundException(); }
    public async Task<Dictionary<string, string>> ResolveAsync(Guid userId, ExecuteRequestDto input, CancellationToken ct)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (input.EnvironmentId.HasValue)
        {
            var environment = await environments.GetAsync(userId, input.EnvironmentId.Value, ct) ?? throw new NotFoundException("The selected environment was not found.");
            foreach (var variable in JsonSerializer.Deserialize<List<EnvironmentVariableDto>>(environment.VariablesJson, JsonDefaults.Web) ?? []) if (variable.Enabled) values[variable.Name] = variable.Value;
        }
        foreach (var pair in input.RuntimeVariables ?? new Dictionary<string, string>()) values[pair.Key] = pair.Value;
        return values;
    }
}
