using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;
using HistoryExecution = ApiTestingPacket.Core.Entities.Execution;

namespace ApiTestingPacket.Service.Services;

public sealed class ExecutionHistoryService(
    IExecutionRepository executions,
    ICollectionRepository collections,
    IRequestRepository requests) : IExecutionHistoryService
{
    public async Task<ExecutionPageDto> ListAsync(Guid userId, string? search, bool? passed, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await executions.ListAsync(userId, search, passed, page, pageSize, ct);
        return new(items.Select(Summary).ToArray(), total, page, pageSize);
    }

    public async Task<HistoryExecution> GetAsync(Guid userId, Guid id, CancellationToken ct) =>
        await executions.GetAsync(userId, id, ct) ?? throw new NotFoundException();

    public Task ClearAsync(Guid userId, CancellationToken ct) => executions.ClearAsync(userId, ct);

    public async Task<DashboardDto> DashboardAsync(Guid userId, CancellationToken ct)
    {
        var (total, passed, failed, average, recent) = await executions.SummaryAsync(userId, ct);
        return new(await collections.CountAsync(userId, ct), await requests.CountAsync(userId, ct), total, passed, failed, average, recent.Select(Summary).ToArray());
    }

    private static ExecutionSummaryDto Summary(HistoryExecution value) =>
        new(value.Id, value.RequestName, value.Method, value.SafeUrl, value.StatusCode, value.DurationMs, value.Passed, value.CreatedAt);
}
