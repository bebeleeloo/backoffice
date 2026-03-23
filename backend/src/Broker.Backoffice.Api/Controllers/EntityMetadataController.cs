using Asp.Versioning;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/entity-metadata")]
[HasPermission(Permissions.SettingsManage)]
public sealed class EntityMetadataController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<EntityMetadataDto>> Get() => Ok(EntityMetadataProvider.GetAll());
}
