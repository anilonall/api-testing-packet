using ApiTestingPacket.Core;

namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record ScenarioInputDto(ScenarioDefinition Definition);
public sealed record ScenarioDto(Guid Id, string Name, string DefinitionJson, DateTimeOffset UpdatedAt);
