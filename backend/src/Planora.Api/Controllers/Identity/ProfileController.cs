using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Profiles;

namespace Planora.Api.Controllers.Identity;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(ProfileService profileService, GmailLinkService gmailLinkService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetMyProfileAsync(CancellationToken cancellationToken) =>
        (await profileService.GetMyProfileAsync(cancellationToken)).ToHttpResult();

    [HttpPut]
    public async Task<IResult> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken) =>
        (await profileService.UpdateMyProfileAsync(request, cancellationToken)).ToHttpResult();

    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IResult> UploadMyAvatarAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Choose an avatar image first."] });

        await using var content = file.OpenReadStream();
        return (await profileService.UploadMyAvatarAsync(
            new UploadMyAvatarRequest(file.FileName, file.ContentType, file.Length, content),
            cancellationToken)).ToHttpResult();
    }

    [HttpPut("preferences")]
    public async Task<IResult> UpdateMyPreferencesAsync(UpdateMyPreferencesRequest request, CancellationToken cancellationToken) =>
        (await profileService.UpdateMyPreferencesAsync(request, cancellationToken)).ToHttpResult();

    [HttpGet("gmail-link")]
    public async Task<IResult> GetMyGmailLinkAsync(CancellationToken cancellationToken) =>
        (await gmailLinkService.GetMyGmailLinkAsync(cancellationToken)).ToHttpResult();

    [HttpPost("gmail-link")]
    public async Task<IResult> LinkMyGmailAsync(LinkMyGmailRequest request, CancellationToken cancellationToken) =>
        (await gmailLinkService.LinkMyGmailAsync(request, cancellationToken)).ToHttpResult();

    [HttpDelete("gmail-link")]
    public async Task<IResult> UnlinkMyGmailAsync(CancellationToken cancellationToken) =>
        (await gmailLinkService.UnlinkMyGmailAsync(cancellationToken)).ToHttpResult();
}
