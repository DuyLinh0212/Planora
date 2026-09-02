namespace Planora.Application.Storage;

public sealed record CreateProjectFolderRequest(Guid? ParentFolderId, string Name);
public sealed record CreateProjectDocumentRequest(Guid FolderId, string Title, string Content, string ContentFormat = "markdown");
public sealed record SaveProjectDocumentRequest(string Content, string ContentFormat = "markdown", string? ChangeNote = null);
public sealed record UploadProjectFileRequest(Guid FolderId, string FileName, string ContentType, long SizeBytes, Stream Content, string? ChangeNote = null);
public sealed record UploadTaskSubmissionFileRequest(string FileName, string ContentType, long SizeBytes, Stream Content);
public sealed record SetFolderAccessRuleRequest(Guid? RoleId, Guid? ProjectMemberId, bool CanView, bool CanCreate, bool CanUpload, bool CanEdit, bool CanDelete);
public sealed record RenameStorageItemRequest(string Name);
public sealed record ProjectFolderResponse(Guid Id, Guid ProjectId, Guid? ParentFolderId, string Name);
public sealed record ProjectDocumentResponse(Guid Id, Guid ProjectId, Guid FolderId, string Title, Guid CurrentVersionId, int VersionNumber, Guid? SourceTaskId = null);
public sealed record DocumentVersionHistoryResponse(
    Guid Id,
    int VersionNumber,
    string Content,
    string ContentFormat,
    Guid EditedByUserId,
    string EditedByDisplayName,
    DateTimeOffset CreatedAt,
    string? ChangeNote);
public sealed record ProjectFileResponse(Guid Id, Guid ProjectId, Guid FolderId, string Name, string MimeType, Guid CurrentVersionId, int VersionNumber, long SizeBytes, Guid? SourceTaskId = null);
public sealed record ProjectStorageResponse(IReadOnlyList<ProjectFolderResponse> Folders, IReadOnlyList<ProjectFileResponse> Files, IReadOnlyList<ProjectDocumentResponse> Documents);
public sealed record FileDownloadResult(Stream Content, string ContentType, string FileName, long SizeBytes);
