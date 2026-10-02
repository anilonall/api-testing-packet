using ApiTestingPacket.Core;

namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record SavedRequestInputDto(Guid CollectionId, Guid? FolderId, RequestDefinition Definition, int SortOrder = 0);
public sealed record SavedRequestDto(Guid Id, Guid? FolderId, string Name, string Method, string Url, string DefinitionJson, int SortOrder, DateTimeOffset UpdatedAt);
public sealed record ExecuteRequestDto(Guid? EnvironmentId, IReadOnlyDictionary<string, string>? RuntimeVariables);
