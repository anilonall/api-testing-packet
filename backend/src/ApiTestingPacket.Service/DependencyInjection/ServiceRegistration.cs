using ApiTestingPacket.Core.Interfaces.Services;
using ApiTestingPacket.Service.Execution;
using ApiTestingPacket.Service.Security;
using ApiTestingPacket.Service.Services;
using ApiTestingPacket.Service.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingPacket.Service.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddSingleton(new ExecutionOptions(
            configuration.GetValue("OutboundRequests:AllowPrivateNetworks", isDevelopment),
            configuration.GetValue("OutboundRequests:MaxResponseBytes", 5 * 1024 * 1024)));
        services.AddSingleton<IOutboundRequestPolicy, OutboundRequestPolicy>();
        services.AddSingleton<AssertionEngine>();
        services.AddScoped<TokenService>();
        services.AddHttpClient<RequestExecutionEngine>().ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseCookies = false
        });
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IEnvironmentService, EnvironmentService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<IExecutionHistoryService, ExecutionHistoryService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        return services;
    }
}
