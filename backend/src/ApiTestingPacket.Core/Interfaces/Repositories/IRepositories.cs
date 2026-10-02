using ApiTestingPacket.Core.Entities;

namespace ApiTestingPacket.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RefreshToken?> FindRefreshTokenAsync(string hash, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> ListAsync(Guid userId, string? search, CancellationToken cancellationToken);
    Task<Collection?> GetAsync(Guid userId, Guid id, bool includeTree, CancellationToken cancellationToken);
    Task AddAsync(Collection collection, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<CollectionFolder?> GetFolderAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<bool> FolderExistsAsync(Guid userId, Guid collectionId, Guid id, CancellationToken cancellationToken);
    Task AddFolderAsync(CollectionFolder folder, CancellationToken cancellationToken);
    Task<bool> DeleteFolderAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IRequestRepository
{
    Task<ApiRequest?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<bool> CollectionExistsAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken);
    Task AddAsync(ApiRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEnvironmentRepository
{
    Task<IReadOnlyList<ApiEnvironment>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiEnvironment?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(ApiEnvironment environment, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IScenarioRepository
{
    Task<IReadOnlyList<Scenario>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<Scenario?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(Scenario scenario, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IExecutionRepository
{
    Task AddAsync(Execution execution, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Execution> executions, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Execution> Items, int Total)> ListAsync(Guid userId, string? search, bool? passed, int page, int pageSize, CancellationToken cancellationToken);
    Task<Execution?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task ClearAsync(Guid userId, CancellationToken cancellationToken);
    Task<(int Total, int Passed, int Failed, double AverageDuration, IReadOnlyList<Execution> Recent)> SummaryAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
