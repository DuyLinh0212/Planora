using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Projects;
using Planora.Domain.Storage;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class ProjectFolderConfiguration : IEntityTypeConfiguration<ProjectFolder>
{
    public void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        builder.ToTable("ProjectFolders");
        builder.HasKey(folder => folder.Id);
        builder.Property(folder => folder.Name).HasMaxLength(260).IsRequired();
        builder.HasIndex(folder => new { folder.ProjectId, folder.ParentFolderId, folder.Name });
        builder.HasQueryFilter(folder => folder.DeletedAt == null);
        builder.HasOne<Project>().WithMany().HasForeignKey(folder => folder.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectFolder>().WithMany().HasForeignKey(folder => folder.ParentFolderId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
{
    public void Configure(EntityTypeBuilder<ProjectFile> builder)
    {
        builder.ToTable("ProjectFiles");
        builder.HasKey(file => file.Id);
        builder.Property(file => file.Name).HasMaxLength(260).IsRequired();
        builder.Property(file => file.MimeType).HasMaxLength(200).IsRequired();
        builder.HasQueryFilter(file => file.DeletedAt == null);
        builder.HasIndex(file => new { file.ProjectId, file.FolderId, file.Name });
        builder.HasOne<Project>().WithMany().HasForeignKey(file => file.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectFolder>().WithMany().HasForeignKey(file => file.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FileVersion>().WithOne().HasForeignKey<ProjectFile>(file => file.CurrentVersionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class FileVersionConfiguration : IEntityTypeConfiguration<FileVersion>
{
    public void Configure(EntityTypeBuilder<FileVersion> builder)
    {
        builder.ToTable("FileVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.CloudinaryPublicId).HasMaxLength(600).IsRequired();
        builder.Property(version => version.CloudinaryResourceType).HasMaxLength(40).IsRequired();
        builder.Property(version => version.Checksum).HasMaxLength(128);
        builder.Property(version => version.ChangeNote).HasMaxLength(1000);
        builder.HasIndex(version => new { version.ProjectFileId, version.VersionNumber }).IsUnique();
        builder.HasOne<ProjectFile>().WithMany().HasForeignKey(version => version.ProjectFileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProjectDocumentConfiguration : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> builder)
    {
        builder.ToTable("ProjectDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Title).HasMaxLength(300).IsRequired();
        builder.HasQueryFilter(document => document.DeletedAt == null);
        builder.HasIndex(document => new { document.ProjectId, document.FolderId, document.Title });
        builder.HasOne<Project>().WithMany().HasForeignKey(document => document.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectFolder>().WithMany().HasForeignKey(document => document.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentVersion>().WithOne().HasForeignKey<ProjectDocument>(document => document.CurrentVersionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Content).IsRequired();
        builder.Property(version => version.ContentFormat).HasMaxLength(40).IsRequired();
        builder.Property(version => version.ChangeNote).HasMaxLength(1000);
        builder.HasIndex(version => new { version.DocumentId, version.VersionNumber }).IsUnique();
        builder.HasOne<ProjectDocument>().WithMany().HasForeignKey(version => version.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FolderAccessRuleConfiguration : IEntityTypeConfiguration<FolderAccessRule>
{
    public void Configure(EntityTypeBuilder<FolderAccessRule> builder)
    {
        builder.ToTable("FolderAccessRules", table => table.HasCheckConstraint(
            "CK_FolderAccessRules_Principal",
            "(\"RoleId\" IS NOT NULL AND \"ProjectMemberId\" IS NULL) OR (\"RoleId\" IS NULL AND \"ProjectMemberId\" IS NOT NULL)"));
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.PrincipalType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(rule => new { rule.FolderId, rule.RoleId, rule.ProjectMemberId });
        builder.HasOne<ProjectFolder>().WithMany().HasForeignKey(rule => rule.FolderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectRole>().WithMany().HasForeignKey(rule => rule.RoleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ProjectMember>().WithMany().HasForeignKey(rule => rule.ProjectMemberId).OnDelete(DeleteBehavior.NoAction);
    }
}
