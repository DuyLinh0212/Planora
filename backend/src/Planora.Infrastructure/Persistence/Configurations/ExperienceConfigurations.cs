using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Administration;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Type).HasMaxLength(60).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(180).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.EntityType).HasMaxLength(80);
        builder.Property(notification => notification.EntityId).HasMaxLength(100);
        builder.HasIndex(notification => new { notification.UserId, notification.DismissedAt, notification.CreatedAt });
        builder.HasIndex(notification => new { notification.UserId, notification.DeletedAt, notification.CreatedAt });
        builder.HasIndex(notification => new { notification.UserId, notification.ReadAt, notification.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupportConversationConfiguration : IEntityTypeConfiguration<SupportConversation>
{
    public void Configure(EntityTypeBuilder<SupportConversation> builder)
    {
        builder.ToTable("SupportConversations");
        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(conversation => conversation.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(conversation => conversation.Subject).HasMaxLength(240).IsRequired();
        builder.HasIndex(conversation => new { conversation.UserId, conversation.Status });
        builder.HasOne<User>().WithMany().HasForeignKey(conversation => conversation.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("SupportMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Content).HasMaxLength(4000).IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
        builder.HasOne<SupportConversation>().WithMany().HasForeignKey(message => message.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(message => message.SenderUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(100).IsRequired();
        builder.Property(setting => setting.Value).HasMaxLength(2000).IsRequired();
        builder.HasIndex(setting => setting.Key).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(setting => setting.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
