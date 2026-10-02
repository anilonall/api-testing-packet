using ApiTestingPacket.Core.DTOs.Auth;
using ApiTestingPacket.Core.Entities;
using ApiTestingPacket.Core.Exceptions;
using ApiTestingPacket.Core.Interfaces.Repositories;
using ApiTestingPacket.Core.Interfaces.Services;
using ApiTestingPacket.Service.Security;

namespace ApiTestingPacket.Service.Services;

public sealed class AuthService(IUserRepository users, TokenService tokens) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email, ct) is not null) throw new ConflictException("An account with this email already exists.");
        var user = new User { Email = email, DisplayName = request.DisplayName.Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12) };
        await users.AddAsync(user, ct); var pair = tokens.Create(user); AddRefresh(user, pair); await users.SaveChangesAsync(ct); return Map(user, pair);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) throw new UnauthorizedAccessException("Invalid email or password.");
        var pair = tokens.Create(user);
        await users.AddRefreshTokenAsync(NewRefresh(user, pair), ct);
        await users.SaveChangesAsync(ct);
        return Map(user, pair);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var old = await users.FindRefreshTokenAsync(TokenService.HashRefresh(refreshToken), ct);
        if (old is null || old.RevokedAt is not null || old.ExpiresAt <= DateTimeOffset.UtcNow) throw new UnauthorizedAccessException("The refresh token is invalid or expired.");
        old.RevokedAt = DateTimeOffset.UtcNow;
        var pair = tokens.Create(old.User);
        var next = NewRefresh(old.User, pair);
        await users.AddRefreshTokenAsync(next, ct);
        old.ReplacedById = next.Id;
        await users.SaveChangesAsync(ct);
        return Map(old.User, pair);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var token = await users.FindRefreshTokenAsync(TokenService.HashRefresh(refreshToken), ct);
        if (token?.UserId == userId) { token.RevokedAt = DateTimeOffset.UtcNow; await users.SaveChangesAsync(ct); }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct) ?? throw new NotFoundException(); return new(user.Id, user.Email, user.DisplayName, user.CreatedAt);
    }

    private static RefreshToken AddRefresh(User user, AuthTokens pair) { var entity = new RefreshToken { User = user, TokenHash = TokenService.HashRefresh(pair.RefreshToken), ExpiresAt = pair.RefreshTokenExpiresAt }; user.RefreshTokens.Add(entity); return entity; }
    private static RefreshToken NewRefresh(User user, AuthTokens pair) => new() { UserId = user.Id, User = user, TokenHash = TokenService.HashRefresh(pair.RefreshToken), ExpiresAt = pair.RefreshTokenExpiresAt };
    private static AuthResponseDto Map(User user, AuthTokens pair) => new(pair.AccessToken, pair.AccessTokenExpiresAt, new UserDto(user.Id, user.Email, user.DisplayName, user.CreatedAt), pair.RefreshToken, pair.RefreshTokenExpiresAt);
}
