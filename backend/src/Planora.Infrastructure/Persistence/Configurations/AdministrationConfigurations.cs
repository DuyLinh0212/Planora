using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planora.Domain.Billing;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Code).HasMaxLength(60).IsRequired();
        builder.Property(plan => plan.Name).HasMaxLength(160).IsRequired();
        builder.Property(plan => plan.Price).HasPrecision(18, 2);
        builder.Property(plan => plan.Currency).HasMaxLength(10).IsRequired();
        builder.Property(plan => plan.BillingPeriod).HasConversion<string>().HasMaxLength(30);
        builder.Property(plan => plan.EntitlementsJson).HasColumnType("text").IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique();
    }
}

public sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(subscription => new { subscription.UserId, subscription.Status });
        builder.HasIndex(subscription => subscription.PaymentTransactionId).IsUnique().HasFilter("\"PaymentTransactionId\" IS NOT NULL");
        builder.HasOne<User>().WithMany().HasForeignKey(subscription => subscription.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(subscription => subscription.PlanId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Provider).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.ProviderOrderId).HasMaxLength(100);
        builder.Property(payment => payment.ProviderTransactionId).HasMaxLength(300);
        builder.Property(payment => payment.CheckoutUrl).HasMaxLength(2000);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(10).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(payment => payment.IdempotencyKey).IsUnique();
        builder.HasIndex(payment => new { payment.Provider, payment.ProviderOrderId }).IsUnique().HasFilter("\"ProviderOrderId\" IS NOT NULL");
        builder.HasIndex(payment => new { payment.Provider, payment.ProviderTransactionId }).IsUnique().HasFilter("\"ProviderTransactionId\" IS NOT NULL");
        builder.HasOne<User>().WithMany().HasForeignKey(payment => payment.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(payment => payment.PlanId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<UserSubscription>().WithMany().HasForeignKey(payment => payment.SubscriptionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(payment => payment.ReviewedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");
        builder.HasKey(feedback => feedback.Id);
        builder.Property(feedback => feedback.Category).HasMaxLength(80).IsRequired();
        builder.Property(feedback => feedback.Subject).HasMaxLength(300).IsRequired();
        builder.Property(feedback => feedback.Content).HasMaxLength(4000).IsRequired();
        builder.Property(feedback => feedback.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(feedback => feedback.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(feedback => feedback.InternalNote).HasMaxLength(4000);
        builder.HasIndex(feedback => new { feedback.Status, feedback.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(feedback => feedback.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(feedback => feedback.AssignedAdminUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
