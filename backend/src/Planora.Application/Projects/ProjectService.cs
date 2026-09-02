using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Storage;
using Planora.Application.Billing;

namespace Planora.Application.Projects;

public sealed class ProjectService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService,
    SubscriptionQuotaService subscriptionQuotaService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<ProjectResponse>> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        if (currentUserId is null)
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.Unauthorized());
        var quotaError = await subscriptionQuotaService.GetOwnedProjectQuotaErrorAsync(currentUserId.Value, cancellationToken);
        if (quotaError is not null)
            return ApplicationResult.Failure<ProjectResponse>(quotaError);
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.Validation("project.name_required", "Project name is required.", "name"));
        if (request.StartAt is not null && request.EndAt is not null && request.StartAt >= request.EndAt)
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.Validation("project.invalid_period", "Project start time must be before end time."));

        var currentTime = timeProvider.GetUtcNow();
        var project = Project.CreateProject(currentUserId.Value, request.Name, request.Description, request.StartAt, request.EndAt, currentTime);
        dbContext.Projects.Add(project);

        var creatorMembership = ProjectMember.AddProjectMember(project.Id, currentUserId.Value, currentTime);
        dbContext.ProjectMembers.Add(creatorMembership);

        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        if (permissions.Count == 0)
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.Conflict("project.permissions_not_seeded", "Permission catalog has not been initialized."));

        var defaultRoles = CreateDefaultProjectRoles(project.Id, currentTime);
        dbContext.ProjectRoles.AddRange(defaultRoles.Values);
        dbContext.ProjectMemberRoles.Add(ProjectMemberRole.CreateProjectMemberRole(creatorMembership.Id, defaultRoles[DefaultProjectRoleCodes.Leader].Id));
        AddDefaultRolePermissions(defaultRoles, permissions);

        dbContext.ProjectFolders.Add(ProjectFolder.CreateProjectFolder(project.Id, null, project.Name, currentUserId.Value, currentTime));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUserId, project.Id, "project.created", nameof(Project), project.Id.ToString(), null, $"{{\"name\":\"{project.Name}\"}}", currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult.Success(MapProjectResponse(project, 1));
    }

    public async Task<ApplicationResult<PagedResponse<ProjectResponse>>> GetProjectsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        if (currentUserId is null)
            return ApplicationResult.Failure<PagedResponse<ProjectResponse>>(ApplicationErrors.Unauthorized());

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var accessibleProjectIds = dbContext.ProjectMembers
            .Where(member => member.UserId == currentUserId && member.Status == MembershipStatus.Active)
            .Select(member => member.ProjectId);
        var projectQuery = dbContext.Projects.Where(project => accessibleProjectIds.Contains(project.Id) && project.DeletedAt == null);
        var totalProjectCount = await projectQuery.CountAsync(cancellationToken);
        var projects = await projectQuery
            .OrderByDescending(project => project.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Description,
                project.Status,
                project.StartAt,
                project.EndAt,
                dbContext.ProjectMembers.Count(member => member.ProjectId == project.Id && member.Status == MembershipStatus.Active),
                project.UpdatedAt))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success(new PagedResponse<ProjectResponse>(projects, totalProjectCount, page, pageSize));
    }

    public async Task<ApplicationResult<ProjectResponse>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        if (currentUserId is null)
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.Unauthorized());
        if (!await projectPermissionService.UserHasPermissionAsync(currentUserId.Value, projectId, PermissionCodes.ProjectView, null, cancellationToken))
            return ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.NotFound("Project"));

        var project = await dbContext.Projects
            .Where(candidate => candidate.Id == projectId && candidate.DeletedAt == null)
            .Select(candidate => new ProjectResponse(
                candidate.Id,
                candidate.Name,
                candidate.Description,
                candidate.Status,
                candidate.StartAt,
                candidate.EndAt,
                dbContext.ProjectMembers.Count(member => member.ProjectId == candidate.Id && member.Status == MembershipStatus.Active),
                candidate.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return project is null
            ? ApplicationResult.Failure<ProjectResponse>(ApplicationErrors.NotFound("Project"))
            : ApplicationResult.Success(project);
    }

    public async Task<ApplicationResult> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectAuthorizationErrorAsync(projectId, PermissionCodes.ProjectEdit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is null || project.DeletedAt is not null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Project"));

        var updateResult = project.UpdateProject(request.Name, request.Description, request.StartAt, request.EndAt, request.Status, timeProvider.GetUtcNow());
        if (!updateResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Validation(updateResult.Code!, updateResult.Message!));

        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectId, "project.updated", nameof(Project), projectId.ToString(), null, null, currentUser.IpAddress, timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectAuthorizationErrorAsync(projectId, PermissionCodes.ProjectDelete, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is null || project.DeletedAt is not null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Project"));

        var currentTime = timeProvider.GetUtcNow();
        project.DeleteProject(currentTime);
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectId, "project.deleted", nameof(Project), projectId.ToString(), null, null, currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> GetProjectAuthorizationErrorAsync(Guid projectId, string permissionCode, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, permissionCode, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Project");
    }

    private static Dictionary<string, ProjectRole> CreateDefaultProjectRoles(Guid projectId, DateTimeOffset createdAt) => new()
    {
        [DefaultProjectRoleCodes.Leader] = ProjectRole.CreateProjectRole(projectId, DefaultProjectRoleCodes.Leader, "Leader", false, createdAt),
        [DefaultProjectRoleCodes.Member] = ProjectRole.CreateProjectRole(projectId, DefaultProjectRoleCodes.Member, "Member", false, createdAt),
        [DefaultProjectRoleCodes.Viewer] = ProjectRole.CreateProjectRole(projectId, DefaultProjectRoleCodes.Viewer, "Viewer", false, createdAt)
    };

    private void AddDefaultRolePermissions(IReadOnlyDictionary<string, ProjectRole> roles, IReadOnlyCollection<Permission> permissions)
    {
        string[] leaderDeniedPermissions = [PermissionCodes.ProjectDelete];
        string[] memberAllowedPermissions = [PermissionCodes.ProjectView, PermissionCodes.SprintView, PermissionCodes.TaskView, PermissionCodes.TaskSubmit, PermissionCodes.TaskRequestExtension, PermissionCodes.FolderView, PermissionCodes.FileView, PermissionCodes.DocumentView];
        string[] viewerAllowedPermissions = [PermissionCodes.ProjectView, PermissionCodes.SprintView, PermissionCodes.TaskView, PermissionCodes.FolderView, PermissionCodes.FileView, PermissionCodes.DocumentView];

        foreach (var permission in permissions)
        {
            if (!leaderDeniedPermissions.Contains(permission.Code, StringComparer.Ordinal))
                dbContext.ProjectRolePermissions.Add(ProjectRolePermission.CreateProjectRolePermission(roles[DefaultProjectRoleCodes.Leader].Id, permission.Id, PermissionEffect.Allow));
            if (memberAllowedPermissions.Contains(permission.Code, StringComparer.Ordinal))
                dbContext.ProjectRolePermissions.Add(ProjectRolePermission.CreateProjectRolePermission(roles[DefaultProjectRoleCodes.Member].Id, permission.Id, PermissionEffect.Allow));
            if (viewerAllowedPermissions.Contains(permission.Code, StringComparer.Ordinal))
                dbContext.ProjectRolePermissions.Add(ProjectRolePermission.CreateProjectRolePermission(roles[DefaultProjectRoleCodes.Viewer].Id, permission.Id, PermissionEffect.Allow));
        }
    }

    private static ProjectResponse MapProjectResponse(Project project, int memberCount) =>
        new(project.Id, project.Name, project.Description, project.Status, project.StartAt, project.EndAt, memberCount, project.UpdatedAt);
}
