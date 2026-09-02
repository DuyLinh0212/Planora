using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Sprints;
using Planora.Domain.Projects;
using Planora.Domain.Tasks;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("Sprints");
        builder.HasKey(sprint => sprint.Id);
        builder.Property(sprint => sprint.Name).HasMaxLength(160).IsRequired();
        builder.Property(sprint => sprint.Goal).HasMaxLength(2000);
        builder.Property(sprint => sprint.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(sprint => new { sprint.ProjectId, sprint.Status });
        builder.HasQueryFilter(sprint => sprint.DeletedAt == null);
        builder.HasOne<Project>().WithMany().HasForeignKey(sprint => sprint.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Title).HasMaxLength(300).IsRequired();
        builder.Property(task => task.Description).HasMaxLength(8000);
        builder.Property(task => task.Type).HasMaxLength(40);
        builder.Property(task => task.SubmissionRequirement).HasConversion<string>().HasMaxLength(40);
        builder.Property(task => task.AllowedExtensionsCsv).HasMaxLength(500);
        builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(task => new { task.ProjectId, task.Status });
        builder.HasIndex(task => new { task.EffectiveDueAt, task.Status });
        builder.HasQueryFilter(task => task.DeletedAt == null);
        builder.HasOne<Project>().WithMany().HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Cascade);
        // SQL Server rejects SetNull here because Project -> Sprint -> Task and
        // Project -> Task form multiple cascade paths. Sprint deletion must move
        // tasks to the backlog explicitly before deleting the sprint.
        builder.HasOne<Sprint>().WithMany().HasForeignKey(task => task.SprintId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("TaskAssignees");
        builder.HasKey(item => new { item.TaskId, item.ProjectMemberId });
        builder.HasIndex(item => new { item.ProjectMemberId, item.TaskId });
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectMember>().WithMany().HasForeignKey(item => item.ProjectMemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaskAcceptanceCriterionConfiguration : IEntityTypeConfiguration<TaskAcceptanceCriterion>
{
    public void Configure(EntityTypeBuilder<TaskAcceptanceCriterion> builder)
    {
        builder.ToTable("TaskAcceptanceCriteria");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Content).HasMaxLength(2000).IsRequired();
        builder.HasIndex(item => new { item.TaskId, item.SortOrder }).IsUnique();
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskExtensionRequestConfiguration : IEntityTypeConfiguration<TaskExtensionRequest>
{
    public void Configure(EntityTypeBuilder<TaskExtensionRequest> builder)
    {
        builder.ToTable("TaskExtensionRequests");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.ReviewNote).HasMaxLength(2000);
        builder.HasIndex(item => new { item.TaskId, item.Status });
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskDeadlineChangeConfiguration : IEntityTypeConfiguration<TaskDeadlineChange>
{
    public void Configure(EntityTypeBuilder<TaskDeadlineChange> builder)
    {
        builder.ToTable("TaskDeadlineChanges");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ChangeType).HasConversion<string>().HasMaxLength(40);
        builder.Property(item => item.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(item => new { item.TaskId, item.CreatedAt });
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskExtensionRequest>().WithMany().HasForeignKey(item => item.ExtensionRequestId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TaskSubmissionConfiguration : IEntityTypeConfiguration<TaskSubmission>
{
    public void Configure(EntityTypeBuilder<TaskSubmission> builder)
    {
        builder.ToTable("TaskSubmissions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Description).HasMaxLength(8000);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.ReviewFeedback).HasMaxLength(4000);
        builder.HasIndex(item => new { item.TaskId, item.AttemptNumber }).IsUnique();
        builder.HasIndex(item => new { item.TaskId, item.SubmittedAt });
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(item => item.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskSubmissionLinkConfiguration : IEntityTypeConfiguration<TaskSubmissionLink>
{
    public void Configure(EntityTypeBuilder<TaskSubmissionLink> builder)
    {
        builder.ToTable("TaskSubmissionLinks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Url).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.LinkType).HasMaxLength(60).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(300);
        builder.HasOne<TaskSubmission>().WithMany().HasForeignKey(item => item.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskSubmissionFileConfiguration : IEntityTypeConfiguration<TaskSubmissionFile>
{
    public void Configure(EntityTypeBuilder<TaskSubmissionFile> builder)
    {
        builder.ToTable("TaskSubmissionFiles");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.SubmissionId, item.FileVersionId }).IsUnique();
        builder.HasOne<TaskSubmission>().WithMany().HasForeignKey(item => item.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Domain.Storage.ProjectFile>().WithMany().HasForeignKey(item => item.ProjectFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Storage.FileVersion>().WithMany().HasForeignKey(item => item.FileVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
