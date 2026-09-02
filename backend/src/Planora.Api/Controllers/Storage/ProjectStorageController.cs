using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Storage;

namespace Planora.Api.Controllers.Storage;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ProjectStorageController(ProjectStorageService projectStorageService) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/storage")]
    public async Task<IResult> GetProjectStorageAsync(Guid projectId, [FromQuery] Guid? folderId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetProjectStorageAsync(projectId, folderId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("projects/{projectId:guid}/storage/folders")]
    public async Task<IResult> CreateProjectFolderAsync(Guid projectId, CreateProjectFolderRequest request, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.CreateProjectFolderAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("projects/{projectId:guid}/storage/documents")]
    public async Task<IResult> CreateProjectDocumentAsync(Guid projectId, CreateProjectDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.CreateProjectDocumentAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPut("storage/documents/{documentId:guid}")]
    public async Task<IResult> SaveProjectDocumentVersionAsync(Guid documentId, SaveProjectDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.SaveProjectDocumentVersionAsync(documentId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("storage/documents/{documentId:guid}/versions")]
    public async Task<IResult> GetProjectDocumentHistoryAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetProjectDocumentHistoryAsync(documentId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("projects/{projectId:guid}/storage/files")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UploadProjectFileAsync(Guid projectId, [FromForm] IFormFile file, [FromForm] Guid folderId, [FromForm] string? changeNote, CancellationToken cancellationToken)
    {
        await using var fileStream = file.OpenReadStream();
        var request = new UploadProjectFileRequest(folderId, file.FileName, file.ContentType, file.Length, fileStream, changeNote);
        var result = await projectStorageService.UploadProjectFileAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("tasks/{taskId:guid}/submission-files")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UploadTaskSubmissionFileAsync(Guid taskId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var fileStream = file.OpenReadStream();
        var request = new UploadTaskSubmissionFileRequest(file.FileName, file.ContentType, file.Length, fileStream);
        var result = await projectStorageService.UploadTaskSubmissionFileAsync(taskId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("storage/files/{fileId:guid}/versions")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UploadProjectFileVersionAsync(Guid fileId, [FromForm] IFormFile file, [FromForm] Guid folderId, [FromForm] string? changeNote, CancellationToken cancellationToken)
    {
        await using var fileStream = file.OpenReadStream();
        var request = new UploadProjectFileRequest(folderId, file.FileName, file.ContentType, file.Length, fileStream, changeNote);
        var result = await projectStorageService.UploadProjectFileVersionAsync(fileId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPut("storage/folders/{folderId:guid}/permissions")]
    public async Task<IResult> SetFolderAccessRuleAsync(Guid folderId, SetFolderAccessRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.SetFolderAccessRuleAsync(folderId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPut("storage/folders/{folderId:guid}/name")]
    public async Task<IResult> RenameProjectFolderAsync(Guid folderId, RenameStorageItemRequest request, CancellationToken cancellationToken) =>
        (await projectStorageService.RenameProjectFolderAsync(folderId, request, cancellationToken)).ToHttpResult();

    [HttpDelete("storage/folders/{folderId:guid}")]
    public async Task<IResult> DeleteProjectFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
        (await projectStorageService.DeleteProjectFolderAsync(folderId, cancellationToken)).ToHttpResult();

    [HttpPut("storage/files/{fileId:guid}/name")]
    public async Task<IResult> RenameProjectFileAsync(Guid fileId, RenameStorageItemRequest request, CancellationToken cancellationToken) =>
        (await projectStorageService.RenameProjectFileAsync(fileId, request, cancellationToken)).ToHttpResult();

    [HttpDelete("storage/files/{fileId:guid}")]
    public async Task<IResult> DeleteProjectFileAsync(Guid fileId, CancellationToken cancellationToken) =>
        (await projectStorageService.DeleteProjectFileAsync(fileId, cancellationToken)).ToHttpResult();

    [HttpPut("storage/documents/{documentId:guid}/name")]
    public async Task<IResult> RenameProjectDocumentAsync(Guid documentId, RenameStorageItemRequest request, CancellationToken cancellationToken) =>
        (await projectStorageService.RenameProjectDocumentAsync(documentId, request, cancellationToken)).ToHttpResult();

    [HttpDelete("storage/documents/{documentId:guid}")]
    public async Task<IResult> DeleteProjectDocumentAsync(Guid documentId, CancellationToken cancellationToken) =>
        (await projectStorageService.DeleteProjectDocumentAsync(documentId, cancellationToken)).ToHttpResult();

    [HttpPost("storage/submissions/{submissionId:guid}/attachments/{fileVersionId:guid}")]
    public async Task<IResult> AttachFileVersionToTaskSubmissionAsync(Guid submissionId, Guid fileVersionId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.AttachFileVersionToTaskSubmissionAsync(submissionId, fileVersionId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("storage/files/{fileId:guid}/content")]
    public async Task<IResult> GetFileContentAsync(Guid fileId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetFileContentAsync(fileId, versionId, cancellationToken);
        if (result.IsFailure || result.Value is null)
            return result.ToHttpResult();
        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true);
    }

    [HttpGet("storage/files/{fileId:guid}/download")]
    public async Task<IResult> DownloadFileAsync(Guid fileId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetFileContentAsync(fileId, versionId, cancellationToken);
        if (result.IsFailure || result.Value is null)
            return result.ToHttpResult();
        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true);
    }

    [HttpGet("storage/file-versions/{versionId:guid}/content")]
    public async Task<IResult> GetFileVersionContentAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetFileVersionContentAsync(versionId, cancellationToken);
        if (result.IsFailure || result.Value is null)
            return result.ToHttpResult();
        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true);
    }

    [HttpGet("storage/file-versions/{versionId:guid}/download")]
    public async Task<IResult> DownloadFileVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await projectStorageService.GetFileVersionContentAsync(versionId, cancellationToken);
        if (result.IsFailure || result.Value is null)
            return result.ToHttpResult();
        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true);
    }
}
