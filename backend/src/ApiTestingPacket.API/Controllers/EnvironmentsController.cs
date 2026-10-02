using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api/environments")]
public sealed class EnvironmentsController(IEnvironmentService environments) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<EnvironmentSummaryDto>> List(CancellationToken ct) => environments.ListAsync(User.UserId(), ct);
    [HttpPost] public async Task<ActionResult> Create(EnvironmentInputDto input, CancellationToken ct) { var value = await environments.CreateAsync(User.UserId(), input, ct); return Created($"/api/environments/{value.Id}", value); }
    [HttpPut("{id:guid}")] public Task<ApiEnvironment> Update(Guid id, EnvironmentInputDto input, CancellationToken ct) => environments.UpdateAsync(User.UserId(), id, input, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await environments.DeleteAsync(User.UserId(), id, ct); return NoContent(); }
}
