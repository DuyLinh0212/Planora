using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDismissalAndAlignProjectRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DismissedAt",
                table: "UserNotifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_DismissedAt_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "UserId", "DismissedAt", "CreatedAt" });

            // Project ownership remains on Projects.OwnerUserId for billing and
            // lifecycle purposes. Workspace membership uses Leader instead.
            migrationBuilder.Sql(
                """
                DELETE pmr
                FROM ProjectMemberRoles pmr
                INNER JOIN ProjectMembers pm ON pm.Id = pmr.ProjectMemberId
                INNER JOIN Projects p ON p.Id = pm.ProjectId AND p.OwnerUserId = pm.UserId
                INNER JOIN ProjectRoles ownerRole ON ownerRole.Id = pmr.RoleId AND ownerRole.Code = 'OWNER';

                INSERT INTO ProjectMemberRoles (ProjectMemberId, RoleId)
                SELECT pm.Id, leaderRole.Id
                FROM ProjectMembers pm
                INNER JOIN Projects p ON p.Id = pm.ProjectId AND p.OwnerUserId = pm.UserId
                INNER JOIN ProjectRoles leaderRole ON leaderRole.ProjectId = p.Id AND leaderRole.Code = 'LEADER'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ProjectMemberRoles existing
                    WHERE existing.ProjectMemberId = pm.Id AND existing.RoleId = leaderRole.Id
                );

                DELETE rolePermission
                FROM ProjectRolePermissions rolePermission
                INNER JOIN ProjectRoles role ON role.Id = rolePermission.RoleId
                WHERE role.Code = 'LEADER';

                INSERT INTO ProjectRolePermissions (RoleId, PermissionId, Effect)
                SELECT role.Id, permission.Id, 'Allow'
                FROM ProjectRoles role
                CROSS JOIN Permissions permission
                WHERE role.Code = 'LEADER' AND permission.Code <> 'project.delete';

                DELETE rolePermission
                FROM ProjectRolePermissions rolePermission
                INNER JOIN ProjectRoles role ON role.Id = rolePermission.RoleId
                INNER JOIN Permissions permission ON permission.Id = rolePermission.PermissionId
                WHERE role.Code = 'MEMBER'
                  AND permission.Code NOT IN (
                    'project.view', 'sprint.view', 'task.view', 'task.submit',
                    'task.request_extension', 'folder.view', 'file.view', 'document.view'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_DismissedAt_CreatedAt",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "DismissedAt",
                table: "UserNotifications");
        }
    }
}
