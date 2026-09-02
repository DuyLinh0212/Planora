using Planora.Domain.Billing;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.UnitTests;

public sealed class AdministrationDomainTests
{
    [Fact]
    public void AssignSystemRole_WithAdministratorRole_UpdatesUserRoleAndTimestamp()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var user = User.CreateUser("admin@planora.com", "ADMIN@PLANORA.COM", "System Admin", createdAt);

        // Act
        user.AssignSystemRole(SystemRole.SystemAdministrator, updatedAt);

        // Assert
        Assert.Equal(SystemRole.SystemAdministrator, user.SystemRole);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }

    [Fact]
    public void UpdateSubscriptionPlan_WithNewQuotas_UpdatesPlanConfiguration()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var plan = SubscriptionPlan.CreateSubscriptionPlan(
            "PRO_MONTHLY",
            "Pro Monthly",
            9m,
            "USD",
            BillingPeriod.Monthly,
            20,
            50L * 1024 * 1024 * 1024,
            "[]",
            createdAt);

        // Act
        plan.UpdateSubscriptionPlan(
            "Pro Monthly Plus",
            12m,
            "USD",
            BillingPeriod.Monthly,
            30,
            100L * 1024 * 1024 * 1024,
            "[\"analytics\"]",
            true,
            updatedAt);

        // Assert
        Assert.Equal("Pro Monthly Plus", plan.Name);
        Assert.Equal(30, plan.MaxOwnedProjects);
        Assert.Equal(100L * 1024 * 1024 * 1024, plan.MaxStorageBytes);
        Assert.Equal(updatedAt, plan.UpdatedAt);
    }

    [Fact]
    public void MarkPaymentTransactionSucceeded_WithVerifiedPayment_StoresProviderAndSubscriptionReferences()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var paidAt = createdAt.AddMinutes(3);
        var subscriptionId = Guid.CreateVersion7();
        var payment = PaymentTransaction.CreatePaymentTransaction(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            PaymentProvider.Momo,
            9m,
            "USD",
            "idem-001",
            createdAt);

        // Act
        payment.MarkPaymentTransactionSucceeded("momo-transaction-001", subscriptionId, paidAt);

        // Assert
        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal("momo-transaction-001", payment.ProviderTransactionId);
        Assert.Equal(subscriptionId, payment.SubscriptionId);
        Assert.Equal(paidAt, payment.PaidAt);
    }

    [Fact]
    public void ResolveFeedback_WithInternalNote_ClosesFeedbackAndPreservesResolutionContext()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var resolvedAt = createdAt.AddHours(2);
        var administratorUserId = Guid.CreateVersion7();
        var feedback = Feedback.CreateFeedback(
            Guid.CreateVersion7(),
            "Bug report",
            "Cannot upload file",
            "Upload stops after validation.",
            FeedbackPriority.High,
            createdAt);
        feedback.AssignFeedbackToAdministrator(administratorUserId);

        // Act
        feedback.ResolveFeedback("Validated storage configuration and informed the user.", resolvedAt);

        // Assert
        Assert.Equal(FeedbackStatus.Resolved, feedback.Status);
        Assert.Equal(administratorUserId, feedback.AssignedAdminUserId);
        Assert.Equal(resolvedAt, feedback.ResolvedAt);
        Assert.NotNull(feedback.InternalNote);
    }
}
