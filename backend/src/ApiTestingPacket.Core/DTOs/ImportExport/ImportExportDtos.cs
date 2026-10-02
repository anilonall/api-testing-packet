using ApiTestingPacket.Core;

namespace ApiTestingPacket.Core.DTOs.Workspace;

public sealed record NativeImportRequestDto(PacketFile Packet, bool IncludeEnvironments = false);
public sealed record OpenApiImportRequestDto(string Document, string Format, string? CollectionName);
public sealed record ImportResultDto(Guid CollectionId, int RequestCount, IReadOnlyList<string> Warnings);
