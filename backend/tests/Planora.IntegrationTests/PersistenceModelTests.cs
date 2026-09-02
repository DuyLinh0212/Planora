using Microsoft.EntityFrameworkCore;
using Planora.Domain.Sprints;
using Planora.Domain.Tasks;
using Planora.Domain.Users;
using Planora.Infrastructure.Persistence;

namespace Planora.IntegrationTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void DatabaseModelContainsCoreBusinessModules()
    {
        using var dbContext = CreateDbContext();
        var entityNames = dbContext.Model.GetEntityTypes().Select(entityType => entityType.ClrType.Name).ToHashSet();
        Assert.Contains("User", entityNames);
        Assert.Contains("Project", entityNames);
        Assert.Contains("Sprint", entityNames);
        Assert.Contains("ProjectTask", entityNames);
        Assert.Contains("ProjectFile", entityNames);
        Assert.Contains("ProjectDocument", entityNames);
    }

    [Fact]
    public void SprintTaskRelationshipDoesNotCreateMultipleCascadePaths()
    {
        using var dbContext = CreateDbContext();
        var taskEntity = dbContext.Model.FindEntityType(typeof(ProjectTask));
        var sprintForeignKey = Assert.Single(taskEntity!.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Sprint));
        Assert.Equal(DeleteBehavior.NoAction, sprintForeignKey.DeleteBehavior);
    }

    [Fact]
    public void DatabaseModel_WithAdministrationModules_ContainsBillingAndSupportEntities()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        // Act
        var entityNames = dbContext.Model.GetEntityTypes().Select(entityType => entityType.ClrType.Name).ToHashSet();

        // Assert
        Assert.Contains("SubscriptionPlan", entityNames);
        Assert.Contains("UserSubscription", entityNames);
        Assert.Contains("PaymentTransaction", entityNames);
        Assert.Contains("Feedback", entityNames);
        Assert.Contains("UserNotification", entityNames);
        Assert.Contains("SupportConversation", entityNames);
        Assert.Contains("SupportMessage", entityNames);
        Assert.Contains("SystemSetting", entityNames);
    }

    [Fact]
    public void PasswordResetToken_HashIndex_IsUnique()
    {
        using var dbContext = CreateDbContext();

        var tokenEntity = dbContext.Model.FindEntityType(typeof(PasswordResetToken));
        var tokenHashProperty = tokenEntity!.FindProperty(nameof(PasswordResetToken.TokenHash));
        var tokenHashIndex = Assert.Single(
            tokenEntity.GetIndexes(),
            index => index.Properties.Contains(tokenHashProperty!));

        Assert.True(tokenHashIndex.IsUnique);
        Assert.Equal(128, tokenHashProperty!.GetMaxLength());
    }

    [Fact]
    public void User_EmailTaskNotificationsEnabled_DefaultsToOffUntilGmailIsLinked()
    {
        using var dbContext = CreateDbContext();

        var emailPreference = dbContext.Model.FindEntityType(typeof(User))!
            .FindProperty(nameof(User.EmailTaskNotificationsEnabled));

        Assert.NotNull(emailPreference);
        Assert.False(emailPreference!.IsNullable);
        Assert.Equal(false, emailPreference.GetDefaultValue());
    }

    private static PlanoraDbContext CreateDbContext() => new(new DbContextOptionsBuilder<PlanoraDbContext>()
        .UseNpgsql("Host=localhost;Port=5432;Database=planora_model_test;Username=postgres;Password=postgres")
        .Options);
}
