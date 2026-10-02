using ApiTestingPacket.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingPacket.Repository.Contexts;

public sealed class ApiTestingPacketDbContext(DbContextOptions<ApiTestingPacketDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionFolder> Folders => Set<CollectionFolder>();
    public DbSet<ApiRequest> ApiRequests => Set<ApiRequest>();
    public DbSet<ApiEnvironment> Environments => Set<ApiEnvironment>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<Execution> Executions => Set<Execution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiTestingPacketDbContext).Assembly);
}
