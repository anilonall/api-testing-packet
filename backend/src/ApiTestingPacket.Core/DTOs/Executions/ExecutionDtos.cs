namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record ExecutionSummaryDto(Guid Id, string RequestName, string Method, string SafeUrl, int? StatusCode, long DurationMs, bool Passed, DateTimeOffset CreatedAt);
public sealed record ExecutionPageDto(IReadOnlyList<ExecutionSummaryDto> Items, int Total, int Page, int PageSize);
public sealed record DashboardDto(int Collections, int Requests, int Executions, int Passed, int Failed, double AverageDurationMs, IReadOnlyList<ExecutionSummaryDto> Recent);
