using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Planora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderUserId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFolders_ProjectFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFolders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRoles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FolderAccessRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanUpload = table.Column<bool>(type: "bit", nullable: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderAccessRules", x => x.Id);
                    table.CheckConstraint("CK_FolderAccessRules_Principal", "([RoleId] IS NOT NULL AND [ProjectMemberId] IS NULL) OR ([RoleId] IS NULL AND [ProjectMemberId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_FolderAccessRules_ProjectFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FolderAccessRules_ProjectMembers_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FolderAccessRules_ProjectRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ProjectRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_ProjectRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ProjectRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberRoles",
                columns: table => new
                {
                    ProjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberRoles", x => new { x.ProjectMemberId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_ProjectMemberRoles_ProjectMembers_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMemberRoles_ProjectRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ProjectRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_ProjectRolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectRolePermissions_ProjectRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ProjectRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EffectiveDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "TaskAcceptanceCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAcceptanceCriteria_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignees",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignees", x => new { x.TaskId, x.ProjectMemberId });
                    table.ForeignKey(
                        name: "FK_TaskAssignees_ProjectMembers_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignees_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskExtensionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExtensionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExtensionRequests_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSubmissions_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDeadlineChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NewDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CountsAsLate = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtensionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDeadlineChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDeadlineChanges_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskDeadlineChanges_TaskExtensionRequests_ExtensionRequestId",
                        column: x => x.ExtensionRequestId,
                        principalTable: "TaskExtensionRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskSubmissionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSubmissionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSubmissionLinks_TaskSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "TaskSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentFormat = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EditedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_DocumentVersions_CurrentVersionId",
                        column: x => x.CurrentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_ProjectFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CloudinaryResourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_FileVersions_CurrentVersionId",
                        column: x => x.CurrentVersionId,
                        principalTable: "FileVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFiles_ProjectFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskSubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSubmissionFiles_FileVersions_FileVersionId",
                        column: x => x.FileVersionId,
                        principalTable: "FileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSubmissionFiles_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSubmissionFiles_TaskSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "TaskSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "project.view", "project", "project view" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "project.edit", "project", "project edit" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "project.delete", "project", "project delete" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "project.manage_members", "project", "project manage members" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "project.manage_roles", "project", "project manage roles" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "project.view_analytics", "project", "project view analytics" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "sprint.view", "sprint", "sprint view" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "sprint.create", "sprint", "sprint create" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "sprint.edit", "sprint", "sprint edit" },
                    { new Guid("10000000-0000-0000-0000-00000000000a"), "sprint.close", "sprint", "sprint close" },
                    { new Guid("10000000-0000-0000-0000-00000000000b"), "task.view", "task", "task view" },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), "task.create", "task", "task create" },
                    { new Guid("10000000-0000-0000-0000-00000000000d"), "task.edit", "task", "task edit" },
                    { new Guid("10000000-0000-0000-0000-00000000000e"), "task.assign", "task", "task assign" },
                    { new Guid("10000000-0000-0000-0000-00000000000f"), "task.submit", "task", "task submit" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "task.review", "task", "task review" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "task.extend_deadline", "task", "task extend deadline" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "task.request_extension", "task", "task request extension" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "folder.view", "folder", "folder view" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "folder.create", "folder", "folder create" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "folder.edit", "folder", "folder edit" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "folder.delete", "folder", "folder delete" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "file.view", "file", "file view" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "file.upload", "file", "file upload" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "file.edit", "file", "file edit" },
                    { new Guid("10000000-0000-0000-0000-00000000001a"), "file.delete", "file", "file delete" },
                    { new Guid("10000000-0000-0000-0000-00000000001b"), "document.view", "document", "document view" },
                    { new Guid("10000000-0000-0000-0000-00000000001c"), "document.edit", "document", "document edit" },
                    { new Guid("10000000-0000-0000-0000-00000000001d"), "document.delete", "document", "document delete" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ProjectId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderUserId",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_ProjectFileId_VersionNumber",
                table: "FileVersions",
                columns: new[] { "ProjectFileId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccessRules_FolderId_RoleId_ProjectMemberId",
                table: "FolderAccessRules",
                columns: new[] { "FolderId", "RoleId", "ProjectMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccessRules_ProjectMemberId",
                table: "FolderAccessRules",
                column: "ProjectMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderAccessRules_RoleId",
                table: "FolderAccessRules",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_CurrentVersionId",
                table: "ProjectDocuments",
                column: "CurrentVersionId",
                unique: true,
                filter: "[CurrentVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_FolderId",
                table: "ProjectDocuments",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId_FolderId_Title",
                table: "ProjectDocuments",
                columns: new[] { "ProjectId", "FolderId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_CurrentVersionId",
                table: "ProjectFiles",
                column: "CurrentVersionId",
                unique: true,
                filter: "[CurrentVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_FolderId",
                table: "ProjectFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_FolderId_Name",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "FolderId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ParentFolderId",
                table: "ProjectFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ProjectId_ParentFolderId_Name",
                table: "ProjectFolders",
                columns: new[] { "ProjectId", "ParentFolderId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_ProjectId_InvitedEmail_Status",
                table: "ProjectInvitations",
                columns: new[] { "ProjectId", "InvitedEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_RoleId",
                table: "ProjectInvitations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TokenHash",
                table: "ProjectInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberRoles_RoleId",
                table: "ProjectMemberRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRolePermissions_PermissionId",
                table: "ProjectRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRoles_ProjectId_Code",
                table: "ProjectRoles",
                columns: new[] { "ProjectId", "Code" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_EffectiveDueAt_Status",
                table: "ProjectTasks",
                columns: new[] { "EffectiveDueAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_Status",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_SprintId",
                table: "ProjectTasks",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId_Status",
                table: "Sprints",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAcceptanceCriteria_TaskId_SortOrder",
                table: "TaskAcceptanceCriteria",
                columns: new[] { "TaskId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignees_ProjectMemberId_TaskId",
                table: "TaskAssignees",
                columns: new[] { "ProjectMemberId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeadlineChanges_ExtensionRequestId",
                table: "TaskDeadlineChanges",
                column: "ExtensionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDeadlineChanges_TaskId_CreatedAt",
                table: "TaskDeadlineChanges",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExtensionRequests_TaskId_Status",
                table: "TaskExtensionRequests",
                columns: new[] { "TaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissionFiles_FileVersionId",
                table: "TaskSubmissionFiles",
                column: "FileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissionFiles_ProjectFileId",
                table: "TaskSubmissionFiles",
                column: "ProjectFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissionFiles_SubmissionId_FileVersionId",
                table: "TaskSubmissionFiles",
                columns: new[] { "SubmissionId", "FileVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissionLinks_SubmissionId",
                table: "TaskSubmissionLinks",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissions_TaskId_AttemptNumber",
                table: "TaskSubmissions",
                columns: new[] { "TaskId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissions_TaskId_SubmittedAt",
                table: "TaskSubmissions",
                columns: new[] { "TaskId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_ProjectDocuments_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId",
                principalTable: "ProjectDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileVersions_ProjectFiles_ProjectFileId",
                table: "FileVersions",
                column: "ProjectFileId",
                principalTable: "ProjectFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_ProjectDocuments_DocumentId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_FileVersions_ProjectFiles_ProjectFileId",
                table: "FileVersions");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "FolderAccessRules");

            migrationBuilder.DropTable(
                name: "ProjectInvitations");

            migrationBuilder.DropTable(
                name: "ProjectMemberRoles");

            migrationBuilder.DropTable(
                name: "ProjectRolePermissions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TaskAcceptanceCriteria");

            migrationBuilder.DropTable(
                name: "TaskAssignees");

            migrationBuilder.DropTable(
                name: "TaskDeadlineChanges");

            migrationBuilder.DropTable(
                name: "TaskSubmissionFiles");

            migrationBuilder.DropTable(
                name: "TaskSubmissionLinks");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "ProjectRoles");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "TaskExtensionRequests");

            migrationBuilder.DropTable(
                name: "TaskSubmissions");

            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropTable(
                name: "ProjectDocuments");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.DropTable(
                name: "ProjectFolders");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
