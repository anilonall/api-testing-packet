namespace ApiTestingPacket.Core.DTOs.Auth;

public sealed record RegisterRequestDto(string Email, string Password, string DisplayName);
public sealed record LoginRequestDto(string Email, string Password);
public sealed record RefreshRequestDto(string? RefreshToken = null);
public sealed record UserDto(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);
public sealed record AuthResponseDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserDto User, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
