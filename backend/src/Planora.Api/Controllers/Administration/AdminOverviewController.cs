using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminOverviewController(AdminOverviewService adminOverviewService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IResult> GetAdminOverviewAsync(CancellationToken cancellationToken)
    {
        var result = await adminOverviewService.GetAdminOverviewAsync(cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("analytics")]
    public async Task<IResult> GetAdminAnalyticsAsync(
        [FromQuery] DateOnly? periodStart,
        [FromQuery] DateOnly? periodEnd,
        CancellationToken cancellationToken)
    {
        var result = await adminOverviewService.GetAdminAnalyticsAsync(periodStart, periodEnd, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("activity")]
    public async Task<IResult> GetAdminActivityAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminOverviewService.GetAdminActivityAsync(page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }
}
