using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiTestingPacket.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiTestingPacket.Service.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "api-testing-packet";
    public string Audience { get; set; } = "api-testing-packet-ui";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 14;
}

public sealed record AuthTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;
    public AuthTokens Create(User user)
    {
        var accessExpiry = DateTimeOffset.UtcNow.AddMinutes(_options.AccessMinutes);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim("name", user.DisplayName)], expires: accessExpiry.UtcDateTime, signingCredentials: credentials);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return new(new JwtSecurityTokenHandler().WriteToken(token), accessExpiry, refresh, DateTimeOffset.UtcNow.AddDays(_options.RefreshDays));
    }

    public static string HashRefresh(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
