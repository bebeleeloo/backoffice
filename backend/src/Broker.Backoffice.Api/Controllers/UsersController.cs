using Asp.Versioning;
using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Users;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/users")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.UsersRead)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromQuery] GetUsersQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.UsersRead)]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetUserByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.UsersCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<UserDto>> Create(CreateUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UsersUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.UsersDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/photo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserPhotoQuery(id), ct);
        Response.Headers.CacheControl = "private, max-age=3600";
        return File(result.Photo, result.ContentType);
    }

    [HttpPut("{id:guid}/photo")]
    [HasPermission(Permissions.UsersUpdate)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        await mediator.Send(new UploadUserPhotoCommand(id, ms.ToArray(), file.ContentType), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/photo")]
    [HasPermission(Permissions.UsersUpdate)]
    public async Task<IActionResult> DeletePhoto(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteUserPhotoCommand(id), ct);
        return NoContent();
    }
}
