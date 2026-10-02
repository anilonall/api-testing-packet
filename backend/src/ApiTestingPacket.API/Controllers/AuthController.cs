using ApiTestingPacket.API.Infrastructure;
using ApiTestingPacket.Core.DTOs.Auth;
using ApiTestingPacket.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiTestingPacket.API.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> Register(RegisterRequestDto request, [FromServices] IValidator<RegisterRequestDto> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(Public(await auth.RegisterAsync(request, ct)));
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> Login(LoginRequestDto request, [FromServices] IValidator<LoginRequestDto> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(Public(await auth.LoginAsync(request, ct)));
    }

    [HttpPost("refresh"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> Refresh(RefreshRequestDto request, CancellationToken ct)
    {
        var value = request.RefreshToken ?? Request.Cookies["atp.refresh"];
        if (string.IsNullOrWhiteSpace(value)) return Unauthorized();
        return Ok(Public(await auth.RefreshAsync(value, ct)));
    }

    [HttpGet("me"), Authorize]
    public Task<UserDto> Me(CancellationToken ct) => auth.GetCurrentUserAsync(User.UserId(), ct);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(RefreshRequestDto request, CancellationToken ct)
    {
        await auth.LogoutAsync(User.UserId(), request.RefreshToken ?? Request.Cookies["atp.refresh"], ct);
        Response.Cookies.Delete("atp.refresh", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    private object Public(AuthResponseDto response)
    {
        Response.Cookies.Append("atp.refresh", response.RefreshToken, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict, Expires = response.RefreshTokenExpiresAt, Path = "/api/auth" });
        return new { response.AccessToken, response.AccessTokenExpiresAt, response.User };
    }
}
