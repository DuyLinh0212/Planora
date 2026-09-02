using Planora.Domain.Common;

namespace Planora.Domain.Storage;

public sealed class ProjectFolder : AuditableEntity
{
    private ProjectFolder() { }
    public Guid ProjectId { get; private set; }
    public Guid? ParentFolderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static ProjectFolder CreateProjectFolder(Guid projectId, Guid? parentFolderId, string name, Guid createdByUserId, DateTimeOffset createdAt)
    {
        var folder = new ProjectFolder { ProjectId = projectId, ParentFolderId = parentFolderId, Name = name.Trim(), CreatedByUserId = createdByUserId };
        folder.MarkCreated(createdAt);
        return folder;
    }

    public void RenameProjectFolder(string name, DateTimeOffset updatedAt)
    {
        Name = name.Trim();
        MarkUpdated(updatedAt);
    }

    public void DeleteProjectFolder(DateTimeOffset deletedAt)
    {
        DeletedAt = deletedAt;
        MarkUpdated(deletedAt);
    }
}
