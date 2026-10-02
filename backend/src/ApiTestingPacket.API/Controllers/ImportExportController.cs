using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class ImportExportController(IImportExportService imports) : ControllerBase
{
    [HttpGet("export/collections/{id:guid}")] public async Task<IActionResult> Export(Guid id, CancellationToken ct) { var file = await imports.ExportCollectionAsync(User.UserId(), id, ct); return File(file.Content, "application/json", file.FileName); }
    [HttpPost("import/native")] public async Task<ActionResult> Native(NativeImportRequestDto input, CancellationToken ct) { var result = await imports.ImportNativeAsync(User.UserId(), input, ct); return Created($"/api/collections/{result.CollectionId}", new { id = result.CollectionId }); }
    [HttpPost("import/openapi")] public async Task<ActionResult> OpenApi(OpenApiImportRequestDto input, CancellationToken ct) { var result = await imports.ImportOpenApiAsync(User.UserId(), input, ct); return Created($"/api/collections/{result.CollectionId}", new { id = result.CollectionId, requestCount = result.RequestCount, result.Warnings }); }
}
