using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Notifications;

namespace Planora.Api.Controllers.Identity;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetMyNotificationsAsync([FromQuery] bool unreadOnly = false, [FromQuery] bool includeDismissed = false, [FromQuery] int? limit = null, CancellationToken cancellationToken = default) =>
        (await notificationService.GetMyNotificationsAsync(unreadOnly, includeDismissed, limit, cancellationToken)).ToHttpResult();

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IResult> MarkMyNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken) =>
        (await notificationService.MarkMyNotificationReadAsync(notificationId, cancellationToken)).ToHttpResult();

    [HttpDelete("{notificationId:guid}")]
    public async Task<IResult> DismissMyNotificationAsync(Guid notificationId, CancellationToken cancellationToken) =>
        (await notificationService.DismissMyNotificationAsync(notificationId, cancellationToken)).ToHttpResult();
}
