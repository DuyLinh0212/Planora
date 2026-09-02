using Microsoft.EntityFrameworkCore;
using Planora.Domain.Billing;
using Planora.Domain.Projects;
using Planora.Domain.Sprints;
using Planora.Domain.Storage;
using Planora.Domain.Tasks;
using Planora.Domain.Users;
using Planora.Domain.Support;
using Planora.Domain.Administration;

namespace Planora.Application.Common.Interfaces;

public interface IPlanoraDbContext
{
    DbSet<User> Users { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<UserGmailLink> UserGmailLinks { get; }
    DbSet<UserNotification> UserNotifications { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectInvitation> ProjectInvitations { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<ProjectRole> ProjectRoles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<ProjectRolePermission> ProjectRolePermissions { get; }
    DbSet<ProjectMemberRole> ProjectMemberRoles { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Sprint> Sprints { get; }
    DbSet<ProjectTask> ProjectTasks { get; }
    DbSet<TaskAssignee> TaskAssignees { get; }
    DbSet<TaskAcceptanceCriterion> TaskAcceptanceCriteria { get; }
    DbSet<TaskExtensionRequest> TaskExtensionRequests { get; }
    DbSet<TaskDeadlineChange> TaskDeadlineChanges { get; }
    DbSet<TaskSubmission> TaskSubmissions { get; }
    DbSet<TaskSubmissionLink> TaskSubmissionLinks { get; }
    DbSet<TaskSubmissionFile> TaskSubmissionFiles { get; }
    DbSet<ProjectFolder> ProjectFolders { get; }
    DbSet<ProjectFile> ProjectFiles { get; }
    DbSet<FileVersion> FileVersions { get; }
    DbSet<ProjectDocument> ProjectDocuments { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<FolderAccessRule> FolderAccessRules { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<Feedback> FeedbackItems { get; }
    DbSet<SupportConversation> SupportConversations { get; }
    DbSet<SupportMessage> SupportMessages { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
