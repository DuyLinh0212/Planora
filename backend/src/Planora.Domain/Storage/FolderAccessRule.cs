using Planora.Domain.Common;

namespace Planora.Domain.Storage;

public enum StoragePrincipalType { Role, Member }

public sealed class FolderAccessRule : Entity
{
    private FolderAccessRule() { }
    public Guid FolderId { get; private set; }
    public StoragePrincipalType PrincipalType { get; private set; }
    public Guid? RoleId { get; private set; }
    public Guid? ProjectMemberId { get; private set; }
    public bool CanView { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanUpload { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDelete { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static FolderAccessRule CreateFolderRuleForRole(Guid folderId, Guid roleId, bool canView, bool canCreate, bool canUpload, bool canEdit, bool canDelete, Guid createdByUserId, DateTimeOffset createdAt) => new()
    {
        FolderId = folderId,
        PrincipalType = StoragePrincipalType.Role,
        RoleId = roleId,
        CanView = canView,
        CanCreate = canCreate,
        CanUpload = canUpload,
        CanEdit = canEdit,
        CanDelete = canDelete,
        CreatedByUserId = createdByUserId,
        CreatedAt = createdAt
    };

    public static FolderAccessRule CreateFolderRuleForMember(Guid folderId, Guid projectMemberId, bool canView, bool canCreate, bool canUpload, bool canEdit, bool canDelete, Guid createdByUserId, DateTimeOffset createdAt) => new()
    {
        FolderId = folderId,
        PrincipalType = StoragePrincipalType.Member,
        ProjectMemberId = projectMemberId,
        CanView = canView,
        CanCreate = canCreate,
        CanUpload = canUpload,
        CanEdit = canEdit,
        CanDelete = canDelete,
        CreatedByUserId = createdByUserId,
        CreatedAt = createdAt
    };
}
