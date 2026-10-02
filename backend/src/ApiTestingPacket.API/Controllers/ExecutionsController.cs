using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class ExecutionsController(IExecutionHistoryService executions) : ControllerBase
{
    [HttpGet("executions")] public Task<ExecutionPageDto> List(string? search, bool? passed, int page = 1, int pageSize = 25, CancellationToken ct = default) => executions.ListAsync(User.UserId(), search, passed, page, pageSize, ct);
    [HttpGet("executions/{id:guid}")] public Task<ApiTestingPacket.Core.Entities.Execution> Get(Guid id, CancellationToken ct) => executions.GetAsync(User.UserId(), id, ct);
    [HttpDelete("executions")] public async Task<IActionResult> Clear(CancellationToken ct) { await executions.ClearAsync(User.UserId(), ct); return NoContent(); }
    [HttpGet("dashboard")] public Task<DashboardDto> Dashboard(CancellationToken ct) => executions.DashboardAsync(User.UserId(), ct);
}
