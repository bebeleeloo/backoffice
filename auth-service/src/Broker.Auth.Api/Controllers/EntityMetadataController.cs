using Asp.Versioning;
using Broker.Auth.Application.Users;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Auth.Api.Controllers;

public sealed record EntityMetadataDto(string Name, List<string> Fields);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth/entity-metadata")]
[HasPermission(Permissions.SettingsManage)]
public sealed class EntityMetadataController : ControllerBase
{
    private static readonly List<EntityMetadataDto> Metadata =
    [
        new("User", typeof(UserDto).GetProperties()
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .Order()
            .ToList())
    ];

    [HttpGet]
    public ActionResult<List<EntityMetadataDto>> Get() => Ok(Metadata);
}
