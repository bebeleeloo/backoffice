using System.Security.Claims;
using Broker.Backoffice.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
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
    public async Task<IActionResult> ChangePassword(ChangePasswordBody body, CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        await mediator.Send(new ChangePasswordCommand(id, body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }

    public sealed record UpdateProfileBody(string? FullName, string Email);

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(UpdateProfileBody body, CancellationToken ct)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        return Ok(await mediator.Send(new UpdateProfileCommand(id, body.FullName, body.Email), ct));
    }

    private bool TryGetUserId(out Guid id)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out id);
    }
}
