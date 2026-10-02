using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api/requests")]
public sealed class RequestsController(IRequestService requests) : ControllerBase
{
    [HttpPost] public async Task<ActionResult> Create(SavedRequestInputDto input, [FromServices] IValidator<SavedRequestInputDto> validator, CancellationToken ct) { await validator.ValidateAndThrowAsync(input, ct); var id = await requests.CreateAsync(User.UserId(), input, ct); return Created($"/api/requests/{id}", new { id }); }
    [HttpPut("{id:guid}")] public async Task<ActionResult> Update(Guid id, SavedRequestInputDto input, [FromServices] IValidator<SavedRequestInputDto> validator, CancellationToken ct) { await validator.ValidateAndThrowAsync(input, ct); await requests.UpdateAsync(User.UserId(), id, input, ct); return Ok(new { id }); }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await requests.DeleteAsync(User.UserId(), id, ct); return NoContent(); }
    [HttpPost("{id:guid}/duplicate")] public async Task<ActionResult> Duplicate(Guid id, CancellationToken ct) { var copyId = await requests.DuplicateAsync(User.UserId(), id, ct); return Created($"/api/requests/{copyId}", new { id = copyId }); }
    [HttpPost("{id:guid}/execute")] public Task<ExecutionResult> Execute(Guid id, ExecuteRequestDto input, CancellationToken ct) => requests.ExecuteAsync(User.UserId(), id, input, ct);
}
