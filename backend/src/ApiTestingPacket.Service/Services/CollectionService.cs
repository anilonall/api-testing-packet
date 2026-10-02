using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;

namespace ApiTestingPacket.Service.Services;

public sealed class CollectionService(ICollectionRepository collections) : ICollectionService
{
    public async Task<IReadOnlyList<CollectionSummaryDto>> ListAsync(Guid userId, string? search, CancellationToken ct) => (await collections.ListAsync(userId, search, ct)).Select(x => new CollectionSummaryDto(x.Id, x.Name, x.Description, x.SortOrder, x.Requests.Count, x.UpdatedAt)).ToArray();
    public async Task<CollectionDetailDto> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var value = await collections.GetAsync(userId, id, true, ct) ?? throw new NotFoundException();
        return new(value.Id, value.Name, value.Description, value.SortOrder, value.Folders.OrderBy(x => x.SortOrder).Select(x => new FolderDto(x.Id, x.ParentId, x.Name, x.SortOrder)).ToArray(), value.Requests.OrderBy(x => x.SortOrder).Select(x => new SavedRequestDto(x.Id, x.FolderId, x.Name, x.Method, x.Url, x.DefinitionJson, x.SortOrder, x.UpdatedAt)).ToArray());
    }
    public async Task<Collection> CreateAsync(Guid userId, CollectionInputDto input, CancellationToken ct) { ValidateName(input.Name); var value = new Collection { UserId = userId, Name = input.Name.Trim(), Description = input.Description?.Trim() ?? "", SortOrder = input.SortOrder }; await collections.AddAsync(value, ct); await collections.SaveChangesAsync(ct); return value; }
    public async Task<Collection> UpdateAsync(Guid userId, Guid id, CollectionInputDto input, CancellationToken ct) { ValidateName(input.Name); var value = await collections.GetAsync(userId, id, false, ct) ?? throw new NotFoundException(); value.Name = input.Name.Trim(); value.Description = input.Description?.Trim() ?? ""; value.SortOrder = input.SortOrder; value.UpdatedAt = DateTimeOffset.UtcNow; await collections.SaveChangesAsync(ct); return value; }
    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct) { if (!await collections.DeleteAsync(userId, id, ct)) throw new NotFoundException(); }
    public async Task<Guid> DuplicateAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var source = await collections.GetAsync(userId, id, true, ct) ?? throw new NotFoundException(); var copy = new Collection { UserId = userId, Name = source.Name + " copy", Description = source.Description, SortOrder = source.SortOrder + 1 }; var folders = source.Folders.ToDictionary(x => x.Id, x => new CollectionFolder { UserId = userId, Collection = copy, Name = x.Name, SortOrder = x.SortOrder }); foreach (var old in source.Folders.Where(x => x.ParentId.HasValue)) folders[old.Id].Parent = folders[old.ParentId!.Value]; foreach (var request in source.Requests) copy.Requests.Add(new ApiRequest { UserId = userId, Name = request.Name, Method = request.Method, Url = request.Url, DefinitionJson = request.DefinitionJson, SortOrder = request.SortOrder, Folder = request.FolderId.HasValue ? folders[request.FolderId.Value] : null }); await collections.AddAsync(copy, ct); await collections.SaveChangesAsync(ct); return copy.Id;
    }
    public async Task<CollectionFolder> CreateFolderAsync(Guid userId, FolderInputDto input, CancellationToken ct) { ValidateName(input.Name); _ = await collections.GetAsync(userId, input.CollectionId, false, ct) ?? throw new NotFoundException(); await ValidateParent(userId, input.CollectionId, input.ParentId, null, ct); var folder = new CollectionFolder { UserId = userId, CollectionId = input.CollectionId, ParentId = input.ParentId, Name = input.Name.Trim(), SortOrder = input.SortOrder }; await collections.AddFolderAsync(folder, ct); await collections.SaveChangesAsync(ct); return folder; }
    public async Task<CollectionFolder> UpdateFolderAsync(Guid userId, Guid id, FolderInputDto input, CancellationToken ct) { ValidateName(input.Name); var folder = await collections.GetFolderAsync(userId, id, ct) ?? throw new NotFoundException(); if (input.CollectionId != folder.CollectionId) throw new InvalidOperationException("A folder cannot be moved to another collection."); await ValidateParent(userId, folder.CollectionId, input.ParentId, id, ct); folder.ParentId = input.ParentId; folder.Name = input.Name.Trim(); folder.SortOrder = input.SortOrder; folder.UpdatedAt = DateTimeOffset.UtcNow; await collections.SaveChangesAsync(ct); return folder; }
    public async Task DeleteFolderAsync(Guid userId, Guid id, CancellationToken ct) { if (!await collections.DeleteFolderAsync(userId, id, ct)) throw new NotFoundException(); }
    private async Task ValidateParent(Guid userId, Guid collectionId, Guid? parentId, Guid? currentId, CancellationToken ct) { if (parentId == currentId || parentId.HasValue && !await collections.FolderExistsAsync(userId, collectionId, parentId.Value, ct)) throw new InvalidOperationException("Parent folder is invalid."); }
    private static void ValidateName(string name) { if (string.IsNullOrWhiteSpace(name)) throw new RequestValidationException(new Dictionary<string, string[]> { ["name"] = ["Name is required."] }); }
}
