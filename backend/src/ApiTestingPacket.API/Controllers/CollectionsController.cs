using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class CollectionsController(ICollectionService collections) : ControllerBase
{
    [HttpGet("collections")] public Task<IReadOnlyList<CollectionSummaryDto>> List(string? search, CancellationToken ct) => collections.ListAsync(User.UserId(), search, ct);
    [HttpGet("collections/{id:guid}")] public Task<CollectionDetailDto> Get(Guid id, CancellationToken ct) => collections.GetAsync(User.UserId(), id, ct);
    [HttpPost("collections")] public async Task<ActionResult> Create(CollectionInputDto input, CancellationToken ct) { var value = await collections.CreateAsync(User.UserId(), input, ct); return Created($"/api/collections/{value.Id}", value); }
    [HttpPut("collections/{id:guid}")] public Task<ApiTestingPacket.Core.Entities.Collection> Update(Guid id, CollectionInputDto input, CancellationToken ct) => collections.UpdateAsync(User.UserId(), id, input, ct);
    [HttpDelete("collections/{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await collections.DeleteAsync(User.UserId(), id, ct); return NoContent(); }
    [HttpPost("collections/{id:guid}/duplicate")] public async Task<ActionResult> Duplicate(Guid id, CancellationToken ct) { var copyId = await collections.DuplicateAsync(User.UserId(), id, ct); return Created($"/api/collections/{copyId}", new { id = copyId }); }
    [HttpPost("folders")] public async Task<ActionResult> CreateFolder(FolderInputDto input, CancellationToken ct) { var value = await collections.CreateFolderAsync(User.UserId(), input, ct); return Created($"/api/folders/{value.Id}", value); }
    [HttpPut("folders/{id:guid}")] public Task<ApiTestingPacket.Core.Entities.CollectionFolder> UpdateFolder(Guid id, FolderInputDto input, CancellationToken ct) => collections.UpdateFolderAsync(User.UserId(), id, input, ct);
    [HttpDelete("folders/{id:guid}")] public async Task<IActionResult> DeleteFolder(Guid id, CancellationToken ct) { await collections.DeleteFolderAsync(User.UserId(), id, ct); return NoContent(); }
}
