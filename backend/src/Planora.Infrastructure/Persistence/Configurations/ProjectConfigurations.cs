using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Application.Authorization;
using Planora.Domain.Users;
using Planora.Domain.Projects;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Name).HasMaxLength(200).IsRequired();
        builder.Property(project => project.Description).HasMaxLength(4000);
        builder.Property(project => project.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(project => project.OwnerUserId);
        builder.HasQueryFilter(project => project.DeletedAt == null);
        builder.HasOne<User>().WithMany().HasForeignKey(project => project.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectInvitationConfiguration : IEntityTypeConfiguration<ProjectInvitation>
{
    public void Configure(EntityTypeBuilder<ProjectInvitation> builder)
    {
        builder.ToTable("ProjectInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.InvitedEmail).HasMaxLength(320).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(invitation => invitation.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
        builder.HasIndex(invitation => new { invitation.ProjectId, invitation.InvitedEmail, invitation.Status });
        builder.HasOne<Project>().WithMany().HasForeignKey(invitation => invitation.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectRole>().WithMany().HasForeignKey(invitation => invitation.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectRoleConfiguration : IEntityTypeConfiguration<ProjectRole>
{
    public void Configure(EntityTypeBuilder<ProjectRole> builder)
    {
        builder.ToTable("ProjectRoles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Code).HasMaxLength(80).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(role => new { role.ProjectId, role.Code }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(role => role.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Code).HasMaxLength(120).IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(160).IsRequired();
        builder.Property(permission => permission.Module).HasMaxLength(80).IsRequired();
        builder.HasIndex(permission => permission.Code).IsUnique();
        builder.HasData(PermissionSeedData.Create());
    }
}

public sealed class ProjectRolePermissionConfiguration : IEntityTypeConfiguration<ProjectRolePermission>
{
    public void Configure(EntityTypeBuilder<ProjectRolePermission> builder)
    {
        builder.ToTable("ProjectRolePermissions");
        builder.HasKey(item => new { item.RoleId, item.PermissionId });
        builder.Property(item => item.Effect).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<ProjectRole>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Permission>().WithMany().HasForeignKey(item => item.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProjectMemberRoleConfiguration : IEntityTypeConfiguration<ProjectMemberRole>
{
    public void Configure(EntityTypeBuilder<ProjectMemberRole> builder)
    {
        builder.ToTable("ProjectMemberRoles");
        builder.HasKey(item => new { item.ProjectMemberId, item.RoleId });
        builder.HasOne<ProjectMember>().WithMany().HasForeignKey(item => item.ProjectMemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectRole>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(160).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(160).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(160).IsRequired();
        builder.Property(log => log.IpAddress).HasMaxLength(80);
        builder.HasIndex(log => new { log.ProjectId, log.CreatedAt });
    }
}

internal static class PermissionSeedData
{
    public static object[] Create() => PermissionCodes.All.Select((code, index) => new
    {
        Id = CreateId(index + 1),
        Code = code,
        Name = code.Replace('_', ' ').Replace('.', ' '),
        Module = code.Split('.')[0]
    }).Cast<object>().ToArray();

    private static Guid CreateId(int value) => Guid.Parse($"10000000-0000-0000-0000-{value:x12}");
}
