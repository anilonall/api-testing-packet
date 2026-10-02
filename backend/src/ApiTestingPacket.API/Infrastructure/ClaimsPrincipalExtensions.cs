using System.Security.Claims;

namespace ApiTestingPacket.API.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("The access token has no valid user identifier.");
}
