using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Storage;
using Planora.Domain.Tasks;
using Planora.Domain.Projects;
using Planora.Application.Billing;

namespace Planora.Application.Storage;

public sealed class ProjectStorageService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService,
    IFileStorage fileStorage,
    SubscriptionQuotaService subscriptionQuotaService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<ProjectStorageResponse>> GetProjectStorageAsync(Guid projectId, Guid? folderId, CancellationToken cancellationToken)
    {
        var rootFolderId = await dbContext.ProjectFolders
            .Where(folder => folder.ProjectId == projectId && folder.ParentFolderId == null && folder.DeletedAt == null)
            .Select(folder => (Guid?)folder.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (rootFolderId is null)
            return ApplicationResult.Failure<ProjectStorageResponse>(ApplicationErrors.NotFound("Project storage"));

        var authorizationError = await GetStorageAuthorizationErrorAsync(projectId, PermissionCodes.FolderView, folderId ?? rootFolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectStorageResponse>(authorizationError);

        var folders = await dbContext.ProjectFolders
            .Where(folder => folder.ProjectId == projectId && folder.DeletedAt == null)
            .Select(folder => new ProjectFolderResponse(folder.Id, folder.ProjectId, folder.ParentFolderId, folder.Name))
            .ToListAsync(cancellationToken);

        var filesQuery = dbContext.ProjectFiles
            .Where(file => file.ProjectId == projectId && file.DeletedAt == null);
        if (folderId.HasValue)
        {
            filesQuery = filesQuery.Where(file => file.FolderId == folderId.Value);
        }

        var files = await (
            from file in filesQuery
            join version in dbContext.FileVersions on file.CurrentVersionId equals version.Id
            select new ProjectFileResponse(file.Id, file.ProjectId, file.FolderId, file.Name, file.MimeType, version.Id, version.VersionNumber, version.SizeBytes, file.SourceTaskId))
            .ToListAsync(cancellationToken);

        var docsQuery = dbContext.ProjectDocuments
            .Where(document => document.ProjectId == projectId && document.DeletedAt == null);
        if (folderId.HasValue)
        {
            docsQuery = docsQuery.Where(document => document.FolderId == folderId.Value);
        }

        var documents = await (
            from document in docsQuery
            join version in dbContext.DocumentVersions on document.CurrentVersionId equals version.Id
            select new ProjectDocumentResponse(document.Id, document.ProjectId, document.FolderId, document.Title, version.Id, version.VersionNumber, document.SourceTaskId))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success(new ProjectStorageResponse(folders, files, documents));
    }

    public async Task<ApplicationResult<ProjectFolderResponse>> CreateProjectFolderAsync(Guid projectId, CreateProjectFolderRequest request, CancellationToken cancellationToken)
    {
        var parentFolderId = request.ParentFolderId ?? await dbContext.ProjectFolders
            .Where(folder => folder.ProjectId == projectId && folder.ParentFolderId == null && folder.DeletedAt == null)
            .Select(folder => (Guid?)folder.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (parentFolderId is null)
            return ApplicationResult.Failure<ProjectFolderResponse>(ApplicationErrors.NotFound("Parent folder"));

        var authorizationError = await GetStorageAuthorizationErrorAsync(projectId, PermissionCodes.FolderCreate, parentFolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectFolderResponse>(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult.Failure<ProjectFolderResponse>(ApplicationErrors.Validation("folder.name_required", "Folder name is required.", "name"));

        var folder = ProjectFolder.CreateProjectFolder(projectId, parentFolderId, request.Name, currentUser.UserId!.Value, timeProvider.GetUtcNow());
        dbContext.ProjectFolders.Add(folder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectFolderResponse(folder.Id, folder.ProjectId, folder.ParentFolderId, folder.Name));
    }

    public async Task<ApplicationResult<ProjectDocumentResponse>> CreateProjectDocumentAsync(Guid projectId, CreateProjectDocumentRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetStorageAuthorizationErrorAsync(projectId, PermissionCodes.DocumentEdit, request.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectDocumentResponse>(authorizationError);
        if (!await FolderBelongsToProjectAsync(request.FolderId, projectId, cancellationToken))
            return ApplicationResult.Failure<ProjectDocumentResponse>(ApplicationErrors.NotFound("Folder"));
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApplicationResult.Failure<ProjectDocumentResponse>(ApplicationErrors.Validation("document.title_required", "Document title is required.", "title"));

        var currentTime = timeProvider.GetUtcNow();
        var document = ProjectDocument.CreateProjectDocument(projectId, request.FolderId, request.Title, currentUser.UserId!.Value, currentTime);
        var documentVersion = DocumentVersion.CreateDocumentVersion(document.Id, 1, request.Content, request.ContentFormat, currentUser.UserId.Value, "Initial version", currentTime);
        dbContext.ProjectDocuments.Add(document);
        // ProjectDocument.CurrentVersionId and DocumentVersion.DocumentId form a
        // real database cycle. Persist the parent first so EF can order both FKs.
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.DocumentVersions.Add(documentVersion);
        document.SetCurrentDocumentVersion(documentVersion.Id, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectDocumentResponse(document.Id, projectId, request.FolderId, document.Title, documentVersion.Id, 1));
    }

    public async Task<ApplicationResult<ProjectDocumentResponse>> SaveProjectDocumentVersionAsync(Guid documentId, SaveProjectDocumentRequest request, CancellationToken cancellationToken)
    {
        var document = await dbContext.ProjectDocuments.FindAsync([documentId], cancellationToken);
        if (document is null || document.DeletedAt is not null)
            return ApplicationResult.Failure<ProjectDocumentResponse>(ApplicationErrors.NotFound("Document"));

        var userCanEditDocument = document.OwnerUserId == currentUser.UserId || await projectPermissionService.UserHasPermissionAsync(currentUser.UserId ?? Guid.Empty, document.ProjectId, PermissionCodes.DocumentEdit, document.FolderId, cancellationToken);
        if (!userCanEditDocument)
            return ApplicationResult.Failure<ProjectDocumentResponse>(ApplicationErrors.Forbidden());

        var nextVersionNumber = await dbContext.DocumentVersions.Where(version => version.DocumentId == documentId).MaxAsync(version => (int?)version.VersionNumber, cancellationToken) + 1 ?? 1;
        var currentTime = timeProvider.GetUtcNow();
        var documentVersion = DocumentVersion.CreateDocumentVersion(documentId, nextVersionNumber, request.Content, request.ContentFormat, currentUser.UserId!.Value, request.ChangeNote, currentTime);
        document.SetCurrentDocumentVersion(documentVersion.Id, currentTime);
        dbContext.DocumentVersions.Add(documentVersion);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectDocumentResponse(document.Id, document.ProjectId, document.FolderId, document.Title, documentVersion.Id, nextVersionNumber));
    }

    public async Task<ApplicationResult<IReadOnlyList<DocumentVersionHistoryResponse>>> GetProjectDocumentHistoryAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.ProjectDocuments.FirstOrDefaultAsync(item => item.Id == documentId && item.DeletedAt == null, cancellationToken);
        if (document is null)
            return ApplicationResult.Failure<IReadOnlyList<DocumentVersionHistoryResponse>>(ApplicationErrors.NotFound("Document"));

        var authorizationError = await GetStorageAuthorizationErrorAsync(document.ProjectId, PermissionCodes.DocumentView, document.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<DocumentVersionHistoryResponse>>(authorizationError);

        var versions = await (
            from version in dbContext.DocumentVersions
            join editor in dbContext.Users on version.EditedByUserId equals editor.Id
            where version.DocumentId == documentId
            orderby version.VersionNumber descending
            select new DocumentVersionHistoryResponse(
                version.Id,
                version.VersionNumber,
                version.Content,
                version.ContentFormat,
                version.EditedByUserId,
                editor.DisplayName,
                version.CreatedAt,
                version.ChangeNote))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success<IReadOnlyList<DocumentVersionHistoryResponse>>(versions);
    }

    public async Task<ApplicationResult<ProjectFileResponse>> UploadProjectFileAsync(Guid projectId, UploadProjectFileRequest request, CancellationToken cancellationToken)
    {
        var fileValidationError = ValidateProjectFile(request);
        if (fileValidationError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(fileValidationError);
        var authorizationError = await GetStorageAuthorizationErrorAsync(projectId, PermissionCodes.FileUpload, request.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(authorizationError);
        if (!await FolderBelongsToProjectAsync(request.FolderId, projectId, cancellationToken))
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.NotFound("Folder"));
        var quotaError = await subscriptionQuotaService.GetUploadQuotaErrorAsync(projectId, request.SizeBytes, cancellationToken);
        if (quotaError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(quotaError);

        var currentTime = timeProvider.GetUtcNow();
        var projectFile = ProjectFile.CreateProjectFile(projectId, request.FolderId, Path.GetFileName(request.FileName), request.ContentType, currentUser.UserId!.Value, currentTime);
        var uploadedFile = await fileStorage.UploadFileAsync(projectId, projectFile.Id, 1, projectFile.Name, projectFile.MimeType, request.Content, cancellationToken);
        if (uploadedFile.IsFailure || uploadedFile.Value is null)
            return ApplicationResult.Failure<ProjectFileResponse>(uploadedFile.Errors.ToArray());

        var fileVersion = FileVersion.CreateFileVersion(projectFile.Id, 1, uploadedFile.Value.PublicId, uploadedFile.Value.ResourceType, uploadedFile.Value.SizeBytes, uploadedFile.Value.Checksum, currentUser.UserId.Value, request.ChangeNote, currentTime);
        dbContext.ProjectFiles.Add(projectFile);
        // ProjectFile.CurrentVersionId and FileVersion.ProjectFileId form a real
        // database cycle. Persist the parent before linking its first version.
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.FileVersions.Add(fileVersion);
        projectFile.SetCurrentFileVersion(fileVersion.Id, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectFileResponse(projectFile.Id, projectId, request.FolderId, projectFile.Name, projectFile.MimeType, fileVersion.Id, 1, fileVersion.SizeBytes));
    }

    public async Task<ApplicationResult<ProjectFileResponse>> UploadTaskSubmissionFileAsync(Guid taskId, UploadTaskSubmissionFileRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken);
        if (projectTask is null || currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.NotFound("Task"));
        if (!await projectPermissionService.UserHasPermissionAsync(userId, projectTask.ProjectId, PermissionCodes.TaskSubmit, null, cancellationToken) ||
            !await CurrentUserIsTaskAssigneeAsync(taskId, userId, cancellationToken))
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.NotFound("Task"));
        if (projectTask.Status is not (PlanoraTaskStatus.Todo or PlanoraTaskStatus.InProgress or PlanoraTaskStatus.Rework))
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.Conflict("task.cannot_submit", "Task is not in a submittable state."));
        if (projectTask.SubmissionRequirement == SubmissionRequirement.LinkOnly)
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.Validation("submission.file_not_allowed", "This task accepts links only.", "file"));

        var extension = Path.GetExtension(request.FileName).TrimStart('.').ToLowerInvariant();
        var allowedExtensions = projectTask.AllowedExtensionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (allowedExtensions.Length > 0 && !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.Validation("submission.file_type_not_allowed", $"This task accepts: {string.Join(", ", allowedExtensions)}.", "file"));
        if (!SubmissionRequirementAcceptsExtension(projectTask.SubmissionRequirement, extension))
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.Validation("submission.file_type_not_allowed", $"The selected file does not match the {projectTask.SubmissionRequirement} submission requirement.", "file"));

        var rootFolderId = await dbContext.ProjectFolders
            .Where(folder => folder.ProjectId == projectTask.ProjectId && folder.ParentFolderId == null && folder.DeletedAt == null)
            .Select(folder => (Guid?)folder.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (rootFolderId is null)
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.NotFound("Project storage"));

        var targetFolderId = rootFolderId.Value;
        var now = timeProvider.GetUtcNow();
        if (projectTask.Type.Equals(nameof(ProjectTaskType.Documentation), StringComparison.OrdinalIgnoreCase))
        {
            var documentsFolderId = await dbContext.ProjectFolders
                .Where(folder => folder.ProjectId == projectTask.ProjectId && folder.ParentFolderId == rootFolderId && folder.DeletedAt == null && folder.Name == "Documents")
                .Select(folder => (Guid?)folder.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (documentsFolderId is null)
            {
                var documentsFolder = ProjectFolder.CreateProjectFolder(projectTask.ProjectId, rootFolderId, "Documents", userId, now);
                dbContext.ProjectFolders.Add(documentsFolder);
                targetFolderId = documentsFolder.Id;
            }
            else
            {
                targetFolderId = documentsFolderId.Value;
            }
        }

        var uploadRequest = new UploadProjectFileRequest(targetFolderId, request.FileName, request.ContentType, request.SizeBytes, request.Content, $"Task submission: {projectTask.Title}");
        var fileValidationError = ValidateProjectFile(uploadRequest);
        if (fileValidationError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(fileValidationError);
        var quotaError = await subscriptionQuotaService.GetUploadQuotaErrorAsync(projectTask.ProjectId, request.SizeBytes, cancellationToken);
        if (quotaError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(quotaError);

        var projectFile = ProjectFile.CreateProjectFile(projectTask.ProjectId, targetFolderId, Path.GetFileName(request.FileName), request.ContentType, userId, now);
        projectFile.SetSourceTask(taskId, now);
        var uploadedFile = await fileStorage.UploadFileAsync(projectTask.ProjectId, projectFile.Id, 1, projectFile.Name, projectFile.MimeType, request.Content, cancellationToken);
        if (uploadedFile.IsFailure || uploadedFile.Value is null)
            return ApplicationResult.Failure<ProjectFileResponse>(uploadedFile.Errors.ToArray());

        var fileVersion = FileVersion.CreateFileVersion(projectFile.Id, 1, uploadedFile.Value.PublicId, uploadedFile.Value.ResourceType, uploadedFile.Value.SizeBytes, uploadedFile.Value.Checksum, userId, uploadRequest.ChangeNote, now);
        dbContext.ProjectFiles.Add(projectFile);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.FileVersions.Add(fileVersion);
        projectFile.SetCurrentFileVersion(fileVersion.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectFileResponse(projectFile.Id, projectTask.ProjectId, targetFolderId, projectFile.Name, projectFile.MimeType, fileVersion.Id, 1, fileVersion.SizeBytes, taskId));
    }

    public async Task<ApplicationResult<ProjectFileResponse>> UploadProjectFileVersionAsync(Guid fileId, UploadProjectFileRequest request, CancellationToken cancellationToken)
    {
        var fileValidationError = ValidateProjectFile(request);
        if (fileValidationError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(fileValidationError);
        var projectFile = await dbContext.ProjectFiles.FindAsync([fileId], cancellationToken);
        if (projectFile is null || projectFile.DeletedAt is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.NotFound("File"));

        var userCanEditFile = projectFile.OwnerUserId == currentUser.UserId || await projectPermissionService.UserHasPermissionAsync(currentUser.UserId ?? Guid.Empty, projectFile.ProjectId, PermissionCodes.FileEdit, projectFile.FolderId, cancellationToken);
        if (!userCanEditFile)
            return ApplicationResult.Failure<ProjectFileResponse>(ApplicationErrors.Forbidden());
        var versionQuotaError = await subscriptionQuotaService.GetFileVersionQuotaErrorAsync(projectFile.ProjectId, projectFile.Id, cancellationToken);
        if (versionQuotaError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(versionQuotaError);
        var uploadQuotaError = await subscriptionQuotaService.GetUploadQuotaErrorAsync(projectFile.ProjectId, request.SizeBytes, cancellationToken);
        if (uploadQuotaError is not null)
            return ApplicationResult.Failure<ProjectFileResponse>(uploadQuotaError);

        var nextVersionNumber = await dbContext.FileVersions.Where(version => version.ProjectFileId == fileId).MaxAsync(version => (int?)version.VersionNumber, cancellationToken) + 1 ?? 1;
        var uploadedFile = await fileStorage.UploadFileAsync(projectFile.ProjectId, projectFile.Id, nextVersionNumber, projectFile.Name, request.ContentType, request.Content, cancellationToken);
        if (uploadedFile.IsFailure || uploadedFile.Value is null)
            return ApplicationResult.Failure<ProjectFileResponse>(uploadedFile.Errors.ToArray());

        var currentTime = timeProvider.GetUtcNow();
        var fileVersion = FileVersion.CreateFileVersion(projectFile.Id, nextVersionNumber, uploadedFile.Value.PublicId, uploadedFile.Value.ResourceType, uploadedFile.Value.SizeBytes, uploadedFile.Value.Checksum, currentUser.UserId!.Value, request.ChangeNote, currentTime);
        projectFile.SetCurrentFileVersion(fileVersion.Id, currentTime);
        dbContext.FileVersions.Add(fileVersion);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new ProjectFileResponse(projectFile.Id, projectFile.ProjectId, projectFile.FolderId, projectFile.Name, projectFile.MimeType, fileVersion.Id, nextVersionNumber, fileVersion.SizeBytes));
    }

    public async Task<ApplicationResult> SetFolderAccessRuleAsync(Guid folderId, SetFolderAccessRuleRequest request, CancellationToken cancellationToken)
    {
        var folder = await dbContext.ProjectFolders.FindAsync([folderId], cancellationToken);
        if (folder is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Folder"));

        var authorizationError = await GetStorageAuthorizationErrorAsync(folder.ProjectId, PermissionCodes.ProjectManageRoles, folderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if ((request.RoleId is null) == (request.ProjectMemberId is null))
            return ApplicationResult.Failure(ApplicationErrors.Validation("folder_rule.invalid_principal", "Specify exactly one role or project member."));

        var currentTime = timeProvider.GetUtcNow();
        var accessRule = request.RoleId is not null
            ? FolderAccessRule.CreateFolderRuleForRole(folderId, request.RoleId.Value, request.CanView, request.CanCreate, request.CanUpload, request.CanEdit, request.CanDelete, currentUser.UserId!.Value, currentTime)
            : FolderAccessRule.CreateFolderRuleForMember(folderId, request.ProjectMemberId!.Value, request.CanView, request.CanCreate, request.CanUpload, request.CanEdit, request.CanDelete, currentUser.UserId!.Value, currentTime);
        dbContext.FolderAccessRules.Add(accessRule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> AttachFileVersionToTaskSubmissionAsync(Guid submissionId, Guid fileVersionId, CancellationToken cancellationToken)
    {
        var taskSubmission = await dbContext.TaskSubmissions.FindAsync([submissionId], cancellationToken);
        var fileVersion = await dbContext.FileVersions.FindAsync([fileVersionId], cancellationToken);
        if (taskSubmission is null || fileVersion is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Submission or file version"));
        if (taskSubmission.SubmittedByUserId != currentUser.UserId)
            return ApplicationResult.Failure(ApplicationErrors.Forbidden());

        var taskProjectId = await dbContext.ProjectTasks.Where(task => task.Id == taskSubmission.TaskId).Select(task => (Guid?)task.ProjectId).FirstOrDefaultAsync(cancellationToken);
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskSubmission.TaskId], cancellationToken);
        var projectFile = await dbContext.ProjectFiles.FindAsync([fileVersion.ProjectFileId], cancellationToken);
        if (projectFile is null || taskProjectId != projectFile.ProjectId)
            return ApplicationResult.Failure(ApplicationErrors.Validation("submission.cross_project_file", "Submission attachment must belong to the same project."));
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));
        if (projectTask.SubmissionRequirement == SubmissionRequirement.LinkOnly)
            return ApplicationResult.Failure(ApplicationErrors.Validation("submission.file_not_allowed", "This task accepts links only.", "fileVersionId"));
        var extension = Path.GetExtension(projectFile.Name).TrimStart('.').ToLowerInvariant();
        var allowedExtensions = projectTask.AllowedExtensionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (allowedExtensions.Length > 0 && !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return ApplicationResult.Failure(ApplicationErrors.Validation("submission.file_type_not_allowed", $"This task accepts: {string.Join(", ", allowedExtensions)}.", "fileVersionId"));
        if (!SubmissionRequirementAcceptsExtension(projectTask.SubmissionRequirement, extension))
            return ApplicationResult.Failure(ApplicationErrors.Validation("submission.file_type_not_allowed", $"The selected file does not match the {projectTask.SubmissionRequirement} submission requirement.", "fileVersionId"));

        var attachmentAlreadyExists = await dbContext.TaskSubmissionFiles.AnyAsync(attachment => attachment.SubmissionId == submissionId && attachment.FileVersionId == fileVersionId, cancellationToken);
        if (!attachmentAlreadyExists)
        {
            dbContext.TaskSubmissionFiles.Add(TaskSubmissionFile.AttachFileVersionToTaskSubmission(submissionId, projectFile.Id, fileVersionId));
            projectFile.SetSourceTask(taskSubmission.TaskId, timeProvider.GetUtcNow());
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> GetStorageAuthorizationErrorAsync(Guid projectId, string permissionCode, Guid? folderId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, permissionCode, folderId, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Storage resource");
    }

    private Task<bool> FolderBelongsToProjectAsync(Guid folderId, Guid projectId, CancellationToken cancellationToken) =>
        dbContext.ProjectFolders.AnyAsync(folder => folder.Id == folderId && folder.ProjectId == projectId && folder.DeletedAt == null, cancellationToken);

    private Task<bool> CurrentUserIsTaskAssigneeAsync(Guid taskId, Guid userId, CancellationToken cancellationToken) =>
        (from assignee in dbContext.TaskAssignees
         join member in dbContext.ProjectMembers on assignee.ProjectMemberId equals member.Id
         where assignee.TaskId == taskId && member.UserId == userId && member.Status == MembershipStatus.Active
         select assignee).AnyAsync(cancellationToken);

    public async Task<ApplicationResult> RenameProjectFolderAsync(Guid folderId, RenameStorageItemRequest request, CancellationToken cancellationToken)
    {
        var folder = await dbContext.ProjectFolders.FirstOrDefaultAsync(item => item.Id == folderId && item.DeletedAt == null, cancellationToken);
        if (folder is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Folder"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(folder.ProjectId, PermissionCodes.FolderEdit, folderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult.Failure(ApplicationErrors.Validation("folder.name_required", "Folder name is required.", "name"));
        folder.RenameProjectFolder(request.Name, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DeleteProjectFolderAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var folder = await dbContext.ProjectFolders.FirstOrDefaultAsync(item => item.Id == folderId && item.DeletedAt == null, cancellationToken);
        if (folder is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Folder"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(folder.ProjectId, PermissionCodes.FolderDelete, folderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (folder.ParentFolderId is null)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("folder.root_protected", "The project root folder cannot be deleted."));
        var hasChildren = await dbContext.ProjectFolders.AnyAsync(item => item.ParentFolderId == folderId && item.DeletedAt == null, cancellationToken)
            || await dbContext.ProjectFiles.AnyAsync(item => item.FolderId == folderId && item.DeletedAt == null, cancellationToken)
            || await dbContext.ProjectDocuments.AnyAsync(item => item.FolderId == folderId && item.DeletedAt == null, cancellationToken);
        if (hasChildren)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("folder.not_empty", "Move or delete every child item before deleting this folder."));
        folder.DeleteProjectFolder(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RenameProjectFileAsync(Guid fileId, RenameStorageItemRequest request, CancellationToken cancellationToken)
    {
        var projectFile = await dbContext.ProjectFiles.FirstOrDefaultAsync(item => item.Id == fileId && item.DeletedAt == null, cancellationToken);
        if (projectFile is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("File"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(projectFile.ProjectId, PermissionCodes.FileEdit, projectFile.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult.Failure(ApplicationErrors.Validation("file.name_required", "File name is required.", "name"));
        projectFile.RenameProjectFile(Path.GetFileName(request.Name), timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DeleteProjectFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var projectFile = await dbContext.ProjectFiles.FirstOrDefaultAsync(item => item.Id == fileId && item.DeletedAt == null, cancellationToken);
        if (projectFile is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("File"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(projectFile.ProjectId, PermissionCodes.FileDelete, projectFile.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var versions = await dbContext.FileVersions.Where(version => version.ProjectFileId == fileId).ToListAsync(cancellationToken);
        foreach (var version in versions)
        {
            var deletion = await fileStorage.DeleteFileAsync(version.CloudinaryPublicId, version.CloudinaryResourceType, cancellationToken);
            if (deletion.IsFailure)
                return ApplicationResult.Failure(deletion.Errors.ToArray());
        }
        // Break the two-way FK cycle before deleting the parent. FileVersion rows
        // are then removed by the ProjectFile -> FileVersion cascade.
        projectFile.ClearCurrentFileVersion();
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ProjectFiles.Remove(projectFile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RenameProjectDocumentAsync(Guid documentId, RenameStorageItemRequest request, CancellationToken cancellationToken)
    {
        var document = await dbContext.ProjectDocuments.FirstOrDefaultAsync(item => item.Id == documentId && item.DeletedAt == null, cancellationToken);
        if (document is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Document"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(document.ProjectId, PermissionCodes.DocumentEdit, document.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult.Failure(ApplicationErrors.Validation("document.title_required", "Document title is required.", "name"));
        document.RenameProjectDocument(request.Name, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DeleteProjectDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.ProjectDocuments.FirstOrDefaultAsync(item => item.Id == documentId && item.DeletedAt == null, cancellationToken);
        if (document is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Document"));
        var authorizationError = await GetStorageAuthorizationErrorAsync(document.ProjectId, PermissionCodes.DocumentDelete, document.FolderId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        document.ClearCurrentDocumentVersion();
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ProjectDocuments.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateProjectFile(UploadProjectFileRequest request)
    {
        if (request.SizeBytes <= 0)
            return ApplicationErrors.Validation("storage.empty_file", "The uploaded file is empty.", "file");
        return null;
    }

    private static bool SubmissionRequirementAcceptsExtension(SubmissionRequirement requirement, string extension) => requirement switch
    {
        SubmissionRequirement.Word => extension is "doc" or "docx",
        SubmissionRequirement.Excel => extension is "xls" or "xlsx" or "csv",
        SubmissionRequirement.Pdf => extension == "pdf",
        SubmissionRequirement.PowerPoint => extension is "ppt" or "pptx",
        SubmissionRequirement.Image => extension is "png" or "jpg" or "jpeg" or "gif" or "webp",
        _ => true
    };

    public async Task<ApplicationResult<FileDownloadResult>> GetFileContentAsync(Guid fileId, Guid? versionId, CancellationToken cancellationToken)
    {
        var projectFile = await dbContext.ProjectFiles.FirstOrDefaultAsync(file => file.Id == fileId && file.DeletedAt == null, cancellationToken);
        if (projectFile is null)
            return ApplicationResult.Failure<FileDownloadResult>(ApplicationErrors.NotFound("File"));

        var authorizationError = await GetFileViewAuthorizationErrorAsync(projectFile, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<FileDownloadResult>(authorizationError);

        var targetVersionId = versionId ?? projectFile.CurrentVersionId;
        var version = await dbContext.FileVersions.FirstOrDefaultAsync(v => v.Id == targetVersionId && v.ProjectFileId == fileId, cancellationToken);
        if (version is null)
            return ApplicationResult.Failure<FileDownloadResult>(ApplicationErrors.NotFound("File version"));

        var streamResult = await fileStorage.GetFileStreamAsync(version.CloudinaryPublicId, version.CloudinaryResourceType, projectFile.Name, cancellationToken);
        if (streamResult.IsFailure || streamResult.Value is null)
            return ApplicationResult.Failure<FileDownloadResult>(streamResult.Errors.ToArray());

        return ApplicationResult.Success(new FileDownloadResult(streamResult.Value, projectFile.MimeType, projectFile.Name, version.SizeBytes));
    }

    public async Task<ApplicationResult<FileDownloadResult>> GetFileVersionContentAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var version = await dbContext.FileVersions.FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
        if (version is null)
            return ApplicationResult.Failure<FileDownloadResult>(ApplicationErrors.NotFound("File version"));

        var projectFile = await dbContext.ProjectFiles.FirstOrDefaultAsync(file => file.Id == version.ProjectFileId && file.DeletedAt == null, cancellationToken);
        if (projectFile is null)
            return ApplicationResult.Failure<FileDownloadResult>(ApplicationErrors.NotFound("File"));

        var authorizationError = await GetFileViewAuthorizationErrorAsync(projectFile, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<FileDownloadResult>(authorizationError);

        var streamResult = await fileStorage.GetFileStreamAsync(version.CloudinaryPublicId, version.CloudinaryResourceType, projectFile.Name, cancellationToken);
        if (streamResult.IsFailure || streamResult.Value is null)
            return ApplicationResult.Failure<FileDownloadResult>(streamResult.Errors.ToArray());

        return ApplicationResult.Success(new FileDownloadResult(streamResult.Value, projectFile.MimeType, projectFile.Name, version.SizeBytes));
    }

    private async Task<ApplicationError?> GetFileViewAuthorizationErrorAsync(ProjectFile projectFile, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();

        var hasDirectPermission = await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectFile.ProjectId, PermissionCodes.FileView, projectFile.FolderId, cancellationToken);
        if (hasDirectPermission)
            return null;

        var isActiveProjectMember = await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectFile.ProjectId && member.UserId == currentUser.UserId.Value && member.Status == MembershipStatus.Active, cancellationToken);
        return isActiveProjectMember ? null : ApplicationErrors.NotFound("File");
    }
}
