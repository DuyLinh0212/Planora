using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;
using Planora.Application.Support;
using Planora.Domain.Support;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/support")]
public sealed class AdminSupportController(SupportConversationAdministrationService supportService) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IResult> GetSupportConversationsAsync([FromQuery] SupportConversationStatus? status, CancellationToken cancellationToken) =>
        (await supportService.GetSupportConversationsAsync(status, cancellationToken)).ToHttpResult();

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IResult> SendAdministratorSupportMessageAsync(Guid conversationId, SendSupportMessageRequest request, CancellationToken cancellationToken) =>
        (await supportService.SendAdministratorSupportMessageAsync(conversationId, request, cancellationToken)).ToHttpResult(StatusCodes.Status201Created);

    [HttpPost("conversations/{conversationId:guid}/close")]
    public async Task<IResult> CloseSupportConversationAsync(Guid conversationId, CancellationToken cancellationToken) =>
        (await supportService.CloseSupportConversationAsync(conversationId, cancellationToken)).ToHttpResult();
}
