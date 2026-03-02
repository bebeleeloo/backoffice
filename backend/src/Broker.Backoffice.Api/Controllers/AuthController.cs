using System.Security.Claims;
using Asp.Versioning;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        var result = await mediator.Send(new GetMeQuery(id), ct);
        return Ok(result);
    }

    public sealed record ChangePasswordBody(string CurrentPassword, string NewPassword);

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> ChangePassword(ChangePasswordBody body, CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        await mediator.Send(new ChangePasswordCommand(id, body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }

    public sealed record UpdateProfileBody(string? FullName, string Email);

    [HttpPut("profile")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(UpdateProfileBody body, CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        return Ok(await mediator.Send(new UpdateProfileCommand(id, body.FullName, body.Email), ct));
    }

    [HttpGet("photo")]
    [Authorize]
    public async Task<IActionResult> GetPhoto(CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        var result = await mediator.Send(new GetUserPhotoQuery(id), ct);
        return File(result.Photo, result.ContentType);
    }

    [HttpPut("photo")]
    [Authorize]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        await mediator.Send(new UploadUserPhotoCommand(id, ms.ToArray(), file.ContentType), ct);
        return NoContent();
    }

    [HttpDelete("photo")]
    [Authorize]
    public async Task<IActionResult> DeletePhoto(CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        await mediator.Send(new DeleteUserPhotoCommand(id), ct);
        return NoContent();
    }

    private bool TryGetUserId(out Guid id)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out id);
    }
}
