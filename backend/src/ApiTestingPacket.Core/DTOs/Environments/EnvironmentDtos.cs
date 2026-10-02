namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record EnvironmentInputDto(string Name, IReadOnlyList<EnvironmentVariableDto> Variables);
public sealed record EnvironmentVariableDto(string Name, string Value, bool Secret = false, bool Enabled = true);
public sealed record EnvironmentSummaryDto(Guid Id, string Name, DateTimeOffset UpdatedAt);
