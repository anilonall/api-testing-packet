using ApiTestingPacket.Core.DTOs.Auth;
using ApiTestingPacket.Core.DTOs.Workspace;
using ApiTestingPacket.Core.Entities;

namespace ApiTestingPacket.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ICollectionService
{
    Task<IReadOnlyList<CollectionSummaryDto>> ListAsync(Guid userId, string? search, CancellationToken cancellationToken);
    Task<CollectionDetailDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Collection> CreateAsync(Guid userId, CollectionInputDto input, CancellationToken cancellationToken);
    Task<Collection> UpdateAsync(Guid userId, Guid id, CollectionInputDto input, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Guid> DuplicateAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<CollectionFolder> CreateFolderAsync(Guid userId, FolderInputDto input, CancellationToken cancellationToken);
    Task<CollectionFolder> UpdateFolderAsync(Guid userId, Guid id, FolderInputDto input, CancellationToken cancellationToken);
    Task DeleteFolderAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public interface IRequestService
{
    Task<Guid> CreateAsync(Guid userId, SavedRequestInputDto input, CancellationToken cancellationToken);
    Task<Guid> UpdateAsync(Guid userId, Guid id, SavedRequestInputDto input, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Guid> DuplicateAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<ExecutionResult> ExecuteAsync(Guid userId, Guid id, ExecuteRequestDto input, CancellationToken cancellationToken);
}

public interface IEnvironmentService
{
    Task<IReadOnlyList<EnvironmentSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiEnvironment> CreateAsync(Guid userId, EnvironmentInputDto input, CancellationToken cancellationToken);
    Task<ApiEnvironment> UpdateAsync(Guid userId, Guid id, EnvironmentInputDto input, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Dictionary<string, string>> ResolveAsync(Guid userId, ExecuteRequestDto input, CancellationToken cancellationToken);
}

public interface IScenarioService
{
    Task<IReadOnlyList<Scenario>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<ScenarioDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Scenario> CreateAsync(Guid userId, ScenarioInputDto input, CancellationToken cancellationToken);
    Task<Scenario> UpdateAsync(Guid userId, Guid id, ScenarioInputDto input, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<ScenarioResult> ExecuteAsync(Guid userId, Guid id, ExecuteRequestDto input, CancellationToken cancellationToken);
}

public interface IExecutionHistoryService
{
    Task<ExecutionPageDto> ListAsync(Guid userId, string? search, bool? passed, int page, int pageSize, CancellationToken cancellationToken);
    Task<Execution> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task ClearAsync(Guid userId, CancellationToken cancellationToken);
    Task<DashboardDto> DashboardAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IImportExportService
{
    Task<(byte[] Content, string FileName)> ExportCollectionAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<ImportResultDto> ImportNativeAsync(Guid userId, NativeImportRequestDto input, CancellationToken cancellationToken);
    Task<ImportResultDto> ImportOpenApiAsync(Guid userId, OpenApiImportRequestDto input, CancellationToken cancellationToken);
}
