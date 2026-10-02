using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Repository.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingPacket.Repository.Repositories;

public sealed class UserRepository(ApiTestingPacketDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<RefreshToken?> FindRefreshTokenAsync(string hash, CancellationToken ct) => db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    public async Task AddAsync(User user, CancellationToken ct) => await db.Users.AddAsync(user, ct);
    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct) => await db.RefreshTokens.AddAsync(token, ct);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

public sealed class CollectionRepository(ApiTestingPacketDbContext db) : ICollectionRepository
{
    public async Task<IReadOnlyList<Collection>> ListAsync(Guid userId, string? search, CancellationToken ct) => await db.Collections.Where(x => x.UserId == userId && (string.IsNullOrEmpty(search) || EF.Functions.ILike(x.Name, $"%{search}%"))).Include(x => x.Requests).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
    public Task<Collection?> GetAsync(Guid userId, Guid id, bool includeTree, CancellationToken ct) { var query = db.Collections.Where(x => x.Id == id && x.UserId == userId); if (includeTree) query = query.Include(x => x.Folders).Include(x => x.Requests); return query.SingleOrDefaultAsync(ct); }
    public async Task AddAsync(Collection value, CancellationToken ct) => await db.Collections.AddAsync(value, ct);
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct) => await db.Collections.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
    public Task<CollectionFolder?> GetFolderAsync(Guid userId, Guid id, CancellationToken ct) => db.Folders.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public Task<bool> FolderExistsAsync(Guid userId, Guid collectionId, Guid id, CancellationToken ct) => db.Folders.AnyAsync(x => x.Id == id && x.CollectionId == collectionId && x.UserId == userId, ct);
    public async Task AddFolderAsync(CollectionFolder folder, CancellationToken ct) => await db.Folders.AddAsync(folder, ct);
    public async Task<bool> DeleteFolderAsync(Guid userId, Guid id, CancellationToken ct) => await db.Folders.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
    public Task<int> CountAsync(Guid userId, CancellationToken ct) => db.Collections.CountAsync(x => x.UserId == userId, ct);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

public sealed class RequestRepository(ApiTestingPacketDbContext db) : IRequestRepository
{
    public Task<ApiRequest?> GetAsync(Guid userId, Guid id, CancellationToken ct) => db.ApiRequests.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public Task<bool> CollectionExistsAsync(Guid userId, Guid collectionId, CancellationToken ct) => db.Collections.AnyAsync(x => x.Id == collectionId && x.UserId == userId, ct);
    public async Task AddAsync(ApiRequest request, CancellationToken ct) => await db.ApiRequests.AddAsync(request, ct);
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct) => await db.ApiRequests.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
    public Task<int> CountAsync(Guid userId, CancellationToken ct) => db.ApiRequests.CountAsync(x => x.UserId == userId, ct);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

public sealed class EnvironmentRepository(ApiTestingPacketDbContext db) : IEnvironmentRepository
{
    public async Task<IReadOnlyList<ApiEnvironment>> ListAsync(Guid userId, CancellationToken ct) => await db.Environments.Where(x => x.UserId == userId).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<ApiEnvironment?> GetAsync(Guid userId, Guid id, CancellationToken ct) => db.Environments.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task AddAsync(ApiEnvironment value, CancellationToken ct) => await db.Environments.AddAsync(value, ct);
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct) => await db.Environments.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

public sealed class ScenarioRepository(ApiTestingPacketDbContext db) : IScenarioRepository
{
    public async Task<IReadOnlyList<Scenario>> ListAsync(Guid userId, CancellationToken ct) => await db.Scenarios.Where(x => x.UserId == userId).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<Scenario?> GetAsync(Guid userId, Guid id, CancellationToken ct) => db.Scenarios.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task AddAsync(Scenario value, CancellationToken ct) => await db.Scenarios.AddAsync(value, ct);
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct) => await db.Scenarios.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(ct) > 0;
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

public sealed class ExecutionRepository(ApiTestingPacketDbContext db) : IExecutionRepository
{
    public async Task AddAsync(Execution value, CancellationToken ct) => await db.Executions.AddAsync(value, ct);
    public async Task AddRangeAsync(IEnumerable<Execution> values, CancellationToken ct) => await db.Executions.AddRangeAsync(values, ct);
    public async Task<(IReadOnlyList<Execution>, int)> ListAsync(Guid userId, string? search, bool? passed, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Executions.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => EF.Functions.ILike(x.RequestName, $"%{search}%") || EF.Functions.ILike(x.SafeUrl, $"%{search}%"));
        if (passed.HasValue) query = query.Where(x => x.Passed == passed);
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return (items, total);
    }
    public Task<Execution?> GetAsync(Guid userId, Guid id, CancellationToken ct) => db.Executions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task ClearAsync(Guid userId, CancellationToken ct) => await db.Executions.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
    public async Task<(int, int, int, double, IReadOnlyList<Execution>)> SummaryAsync(Guid userId, CancellationToken ct)
    {
        var query = db.Executions.Where(x => x.UserId == userId); var total = await query.CountAsync(ct); var passed = await query.CountAsync(x => x.Passed, ct); var average = await query.Select(x => (double?)x.DurationMs).AverageAsync(ct) ?? 0; var recent = await query.OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync(ct); return (total, passed, total - passed, average, recent);
    }
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
