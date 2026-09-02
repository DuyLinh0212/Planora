using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Support;

namespace Planora.Api.Controllers.Support;

[ApiController]
[Authorize]
[Route("api/support")]
public sealed class SupportController(SupportService supportService) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IResult> GetMySupportConversationsAsync(CancellationToken cancellationToken) =>
        (await supportService.GetMySupportConversationsAsync(cancellationToken)).ToHttpResult();

    [HttpPost("conversations")]
    public async Task<IResult> CreateSupportConversationAsync(CreateSupportConversationRequest request, CancellationToken cancellationToken) =>
        (await supportService.CreateSupportConversationAsync(request, cancellationToken)).ToHttpResult(StatusCodes.Status201Created);

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IResult> SendSupportMessageAsync(Guid conversationId, SendSupportMessageRequest request, CancellationToken cancellationToken) =>
        (await supportService.SendSupportMessageAsync(conversationId, request, cancellationToken)).ToHttpResult(StatusCodes.Status201Created);
}
