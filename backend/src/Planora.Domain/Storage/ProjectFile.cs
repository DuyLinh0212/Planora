using Planora.Domain.Common;

namespace Planora.Domain.Storage;

public sealed class ProjectFile : AuditableEntity
{
    private ProjectFile() { }
    public Guid ProjectId { get; private set; }
    public Guid FolderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public Guid? CurrentVersionId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid? SourceTaskId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static ProjectFile CreateProjectFile(Guid projectId, Guid folderId, string name, string mimeType, Guid ownerUserId, DateTimeOffset createdAt)
    {
        var file = new ProjectFile { ProjectId = projectId, FolderId = folderId, Name = name.Trim(), MimeType = mimeType, OwnerUserId = ownerUserId };
        file.MarkCreated(createdAt);
        return file;
    }

    public void SetCurrentFileVersion(Guid versionId, DateTimeOffset updatedAt)
    {
        CurrentVersionId = versionId;
        MarkUpdated(updatedAt);
    }

    public void ClearCurrentFileVersion() => CurrentVersionId = null;

    public void RenameProjectFile(string name, DateTimeOffset updatedAt)
    {
        Name = name.Trim();
        MarkUpdated(updatedAt);
    }

    public void SetSourceTask(Guid taskId, DateTimeOffset updatedAt)
    {
        SourceTaskId = taskId;
        MarkUpdated(updatedAt);
    }
}

public sealed class FileVersion : Entity
{
    private FileVersion() { }
    public Guid ProjectFileId { get; private set; }
    public int VersionNumber { get; private set; }
    public string CloudinaryPublicId { get; private set; } = string.Empty;
    public string CloudinaryResourceType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? Checksum { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? ChangeNote { get; private set; }
    public static FileVersion CreateFileVersion(Guid projectFileId, int versionNumber, string publicId, string resourceType, long sizeBytes, string? checksum, Guid uploadedByUserId, string? changeNote, DateTimeOffset createdAt) => new()
    {
        ProjectFileId = projectFileId,
        VersionNumber = versionNumber,
        CloudinaryPublicId = publicId,
        CloudinaryResourceType = resourceType,
        SizeBytes = sizeBytes,
        Checksum = checksum,
        UploadedByUserId = uploadedByUserId,
        ChangeNote = changeNote,
        CreatedAt = createdAt
    };
}
