using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Repository.Contexts;
using ApiTestingPacket.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingPacket.Repository.DependencyInjection;

public static class RepositoryServiceRegistration
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiTestingPacketDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Database")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        return services;
    }
}
