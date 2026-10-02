namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record CollectionInputDto(string Name, string? Description, int SortOrder = 0);
public sealed record FolderInputDto(Guid CollectionId, Guid? ParentId, string Name, int SortOrder = 0);
public sealed record CollectionSummaryDto(Guid Id, string Name, string Description, int SortOrder, int RequestCount, DateTimeOffset UpdatedAt);
public sealed record CollectionDetailDto(Guid Id, string Name, string Description, int SortOrder, IReadOnlyList<FolderDto> Folders, IReadOnlyList<SavedRequestDto> Requests);
public sealed record FolderDto(Guid Id, Guid? ParentId, string Name, int SortOrder);
