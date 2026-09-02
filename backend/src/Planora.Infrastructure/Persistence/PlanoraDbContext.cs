using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Domain.Billing;
using Planora.Domain.Projects;
using Planora.Domain.Sprints;
using Planora.Domain.Storage;
using Planora.Domain.Tasks;
using Planora.Domain.Users;
using Planora.Domain.Support;
using Planora.Domain.Administration;

namespace Planora.Infrastructure.Persistence;

public sealed class PlanoraDbContext(DbContextOptions<PlanoraDbContext> options) : DbContext(options), IPlanoraDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserGmailLink> UserGmailLinks => Set<UserGmailLink>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectRole> ProjectRoles => Set<ProjectRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<ProjectRolePermission> ProjectRolePermissions => Set<ProjectRolePermission>();
    public DbSet<ProjectMemberRole> ProjectMemberRoles => Set<ProjectMemberRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<TaskAcceptanceCriterion> TaskAcceptanceCriteria => Set<TaskAcceptanceCriterion>();
    public DbSet<TaskExtensionRequest> TaskExtensionRequests => Set<TaskExtensionRequest>();
    public DbSet<TaskDeadlineChange> TaskDeadlineChanges => Set<TaskDeadlineChange>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<TaskSubmissionLink> TaskSubmissionLinks => Set<TaskSubmissionLink>();
    public DbSet<TaskSubmissionFile> TaskSubmissionFiles => Set<TaskSubmissionFile>();
    public DbSet<ProjectFolder> ProjectFolders => Set<ProjectFolder>();
    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();
    public DbSet<FileVersion> FileVersions => Set<FileVersion>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<FolderAccessRule> FolderAccessRules => Set<FolderAccessRule>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Feedback> FeedbackItems => Set<Feedback>();
    public DbSet<SupportConversation> SupportConversations => Set<SupportConversation>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanoraDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
