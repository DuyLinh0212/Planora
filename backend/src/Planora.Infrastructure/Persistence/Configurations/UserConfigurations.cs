using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Users;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(user => user.Username).HasMaxLength(30).IsRequired();
        builder.Property(user => user.NormalizedUsername).HasMaxLength(30).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(1000);
        builder.Property(user => user.AvatarUrl).HasMaxLength(2000);
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(user => user.SystemRole).HasConversion<string>().HasMaxLength(40);
        builder.Property(user => user.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(user => user.ThemePreference).HasMaxLength(20).IsRequired();
        builder.Property(user => user.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(user => user.EmailTaskNotificationsEnabled).HasDefaultValue(false);
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
        builder.HasIndex(user => user.NormalizedUsername).IsUnique();
    }
}

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");
        builder.HasKey(login => login.Id);
        builder.Property(login => login.Provider).HasMaxLength(30).IsRequired();
        builder.Property(login => login.ProviderUserId).HasMaxLength(300).IsRequired();
        builder.HasIndex(login => new { login.Provider, login.ProviderUserId }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(login => login.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.DeviceInfo).HasMaxLength(500);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserGmailLinkConfiguration : IEntityTypeConfiguration<UserGmailLink>
{
    public void Configure(EntityTypeBuilder<UserGmailLink> builder)
    {
        builder.ToTable("UserGmailLinks");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.GmailAddress).HasMaxLength(320).IsRequired();
        builder.Property(link => link.RefreshTokenCipher).HasMaxLength(4000).IsRequired();
        builder.Property(link => link.RefreshTokenNonce).HasMaxLength(64).IsRequired();
        builder.Property(link => link.LastSendFailureReason).HasMaxLength(400);
        builder.HasIndex(link => link.UserId).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(link => link.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
