using Planora.Domain.Common;

namespace Planora.Domain.Storage;

public sealed class ProjectDocument : AuditableEntity
{
    private ProjectDocument() { }
    public Guid ProjectId { get; private set; }
    public Guid FolderId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public Guid? SourceTaskId { get; private set; }
    public Guid? CurrentVersionId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static ProjectDocument CreateProjectDocument(Guid projectId, Guid folderId, string title, Guid ownerUserId, DateTimeOffset createdAt)
    {
        var document = new ProjectDocument { ProjectId = projectId, FolderId = folderId, Title = title.Trim(), OwnerUserId = ownerUserId };
        document.MarkCreated(createdAt);
        return document;
    }

    public void SetCurrentDocumentVersion(Guid versionId, DateTimeOffset updatedAt)
    {
        CurrentVersionId = versionId;
        MarkUpdated(updatedAt);
    }

    public void ClearCurrentDocumentVersion() => CurrentVersionId = null;

    public void RenameProjectDocument(string title, DateTimeOffset updatedAt)
    {
        Title = title.Trim();
        MarkUpdated(updatedAt);
    }
}

public sealed class DocumentVersion : Entity
{
    private DocumentVersion() { }
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string ContentFormat { get; private set; } = "markdown";
    public Guid EditedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? ChangeNote { get; private set; }
    public static DocumentVersion CreateDocumentVersion(Guid documentId, int versionNumber, string content, string contentFormat, Guid editedByUserId, string? changeNote, DateTimeOffset createdAt) => new()
    {
        DocumentId = documentId,
        VersionNumber = versionNumber,
        Content = content,
        ContentFormat = contentFormat,
        EditedByUserId = editedByUserId,
        ChangeNote = changeNote,
        CreatedAt = createdAt
    };
}
