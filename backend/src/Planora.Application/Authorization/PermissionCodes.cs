namespace Planora.Application.Authorization;

public static class PermissionCodes
{
    public const string ProjectView = "project.view";
    public const string ProjectEdit = "project.edit";
    public const string ProjectDelete = "project.delete";
    public const string ProjectManageMembers = "project.manage_members";
    public const string ProjectManageRoles = "project.manage_roles";
    public const string ProjectViewAnalytics = "project.view_analytics";
    public const string SprintView = "sprint.view";
    public const string SprintCreate = "sprint.create";
    public const string SprintEdit = "sprint.edit";
    public const string SprintClose = "sprint.close";
    public const string TaskView = "task.view";
    public const string TaskCreate = "task.create";
    public const string TaskEdit = "task.edit";
    public const string TaskAssign = "task.assign";
    public const string TaskSubmit = "task.submit";
    public const string TaskReview = "task.review";
    public const string TaskExtendDeadline = "task.extend_deadline";
    public const string TaskRequestExtension = "task.request_extension";
    public const string FolderView = "folder.view";
    public const string FolderCreate = "folder.create";
    public const string FolderEdit = "folder.edit";
    public const string FolderDelete = "folder.delete";
    public const string FileView = "file.view";
    public const string FileUpload = "file.upload";
    public const string FileEdit = "file.edit";
    public const string FileDelete = "file.delete";
    public const string DocumentView = "document.view";
    public const string DocumentEdit = "document.edit";
    public const string DocumentDelete = "document.delete";

    public static readonly string[] All =
    [
        ProjectView, ProjectEdit, ProjectDelete, ProjectManageMembers, ProjectManageRoles, ProjectViewAnalytics,
        SprintView, SprintCreate, SprintEdit, SprintClose,
        TaskView, TaskCreate, TaskEdit, TaskAssign, TaskSubmit, TaskReview, TaskExtendDeadline, TaskRequestExtension,
        FolderView, FolderCreate, FolderEdit, FolderDelete,
        FileView, FileUpload, FileEdit, FileDelete,
        DocumentView, DocumentEdit, DocumentDelete
    ];
}
