using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;
using Planora.Domain.Users;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/accounts")]
public sealed class AdminAccountsController(AdminAccountService adminAccountService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetAdminAccountsAsync(
        [FromQuery] string? search,
        [FromQuery] UserStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminAccountService.GetAdminAccountsAsync(search, status, page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("{accountId:guid}")]
    public async Task<IResult> GetAdminAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await adminAccountService.GetAdminAccountByIdAsync(accountId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{accountId:guid}/suspend")]
    public async Task<IResult> SuspendUserAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await adminAccountService.SuspendUserAccountAsync(accountId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{accountId:guid}/restore")]
    public async Task<IResult> RestoreUserAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await adminAccountService.RestoreUserAccountAsync(accountId, cancellationToken);
        return result.ToHttpResult();
    }
}
