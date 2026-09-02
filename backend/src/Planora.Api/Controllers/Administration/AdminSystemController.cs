using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Route("api/system")]
public sealed class AdminSystemController(SystemSettingService systemSettingService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("maintenance")]
    public async Task<IResult> GetMaintenanceStatusAsync(CancellationToken cancellationToken) =>
        (await systemSettingService.GetMaintenanceStatusAsync(cancellationToken)).ToHttpResult();

    [Authorize]
    [HttpPut("maintenance")]
    public async Task<IResult> UpdateMaintenanceStatusAsync(UpdateMaintenanceStatusRequest request, CancellationToken cancellationToken) =>
        (await systemSettingService.UpdateMaintenanceStatusAsync(request, cancellationToken)).ToHttpResult();
}
