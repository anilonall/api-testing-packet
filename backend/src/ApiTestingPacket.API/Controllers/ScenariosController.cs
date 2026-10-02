using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api/scenarios")]
public sealed class ScenariosController(IScenarioService scenarios) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<Scenario>> List(CancellationToken ct) => scenarios.ListAsync(User.UserId(), ct);
    [HttpGet("{id:guid}")] public Task<ScenarioDto> Get(Guid id, CancellationToken ct) => scenarios.GetAsync(User.UserId(), id, ct);
    [HttpPost] public async Task<ActionResult> Create(ScenarioInputDto input, CancellationToken ct) { var value = await scenarios.CreateAsync(User.UserId(), input, ct); return Created($"/api/scenarios/{value.Id}", value); }
    [HttpPut("{id:guid}")] public Task<Scenario> Update(Guid id, ScenarioInputDto input, CancellationToken ct) => scenarios.UpdateAsync(User.UserId(), id, input, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await scenarios.DeleteAsync(User.UserId(), id, ct); return NoContent(); }
    [HttpPost("{id:guid}/execute")] public Task<ScenarioResult> Execute(Guid id, ExecuteRequestDto input, CancellationToken ct) => scenarios.ExecuteAsync(User.UserId(), id, input, ct);
}
