using System.Text;
using System.Threading.RateLimiting;
using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Repository.Contexts;
using ApiTestingPacket.Repository.DependencyInjection;
using ApiTestingPacket.Service.DependencyInjection;
using ApiTestingPacket.Service.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRepositoryServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<ApiTestingPacketDbContext>();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwt.Issuer,
    ValidateAudience = true,
    ValidAudience = jwt.Audience,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
    ClockSkew = TimeSpan.FromSeconds(30)
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();

if (builder.Configuration.GetValue<bool>("ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ApiTestingPacketDbContext>().Database.MigrateAsync();
}

app.Run();
public partial class Program;
