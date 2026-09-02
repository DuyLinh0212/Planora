using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Planora.Application.Authentication;
using Planora.Application.Administration;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.ProjectMembers;
using Planora.Application.Projects;
using Planora.Application.Sprints;
using Planora.Application.Storage;
using Planora.Application.TaskDeadlines;
using Planora.Application.Tasks;
using Planora.Application.TaskSubmissions;
using Planora.Application.Profiles;
using Planora.Application.Notifications;
using Planora.Application.Billing;
using Planora.Application.Support;
using Planora.Infrastructure.Authentication;
using Planora.Infrastructure.ExternalAuth;
using Planora.Infrastructure.Notifications;
using Planora.Infrastructure.Persistence;
using Planora.Infrastructure.Storage;
using Planora.Infrastructure.Payments;

namespace Planora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlanoraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=planora;Username=postgres;Password=CHANGE_ME_POSTGRES_PASSWORD;SSL Mode=Prefer";
        var connectionString = PostgreSqlConnectionString.Normalize(configuredConnectionString);
        services.AddDbContext<PlanoraDbContext>(options =>
            options.UseNpgsql(connectionString, postgreSql =>
                postgreSql
                    .MigrationsAssembly(typeof(PlanoraDbContext).Assembly.FullName)
                    .EnableRetryOnFailure()));
        services.AddScoped<IPlanoraDbContext>(serviceProvider => serviceProvider.GetRequiredService<PlanoraDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<ExternalAuthenticationOptions>(configuration.GetSection("Authentication"));
        services.Configure<PasswordResetNotificationOptions>(configuration.GetSection("PasswordReset"));
        services.Configure<TaskEmailNotificationOptions>(configuration.GetSection("TaskEmailNotifications"));
        services.Configure<GmailIntegrationOptions>(configuration.GetSection("GmailIntegration"));
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<MomoPaymentOptions>(configuration.GetSection("Payment:Momo"));
        services.Configure<BankTransferPaymentOptions>(configuration.GetSection("Payment:BankTransfer"));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher, PlanoraPasswordHasher>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddHttpClient<IExternalIdentityVerifier, ExternalIdentityVerifier>(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IPasswordResetNotificationSender, SmtpPasswordResetNotificationSender>();
        services.AddSingleton<TaskEmailNotificationQueue>();
        services.AddSingleton<ITaskEmailNotificationQueue>(serviceProvider => serviceProvider.GetRequiredService<TaskEmailNotificationQueue>());
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddHttpClient<IGmailOAuthClient, GmailOAuthClient>(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<IGmailMessageSender, GmailMessageSender>(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient<IMomoPaymentGateway, MomoPaymentGateway>(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<IBankTransferPaymentDetailsProvider, BankTransferPaymentDetailsProvider>();
        services.AddScoped<ITaskEmailNotificationSender, TaskEmailNotificationDispatcher>();
        services.AddScoped<CloudinaryFileStorage>();
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<IFileStorage>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<CloudinaryOptions>>().Value.IsConfigured
                ? serviceProvider.GetRequiredService<CloudinaryFileStorage>()
                : serviceProvider.GetRequiredService<LocalFileStorage>());
        services.AddSingleton<IStoragePolicy, ConfiguredStoragePolicy>();

        services.AddScoped<IProjectPermissionService, ProjectPermissionService>();
        services.AddScoped(serviceProvider => new AuthenticationService(
            serviceProvider.GetRequiredService<IPlanoraDbContext>(),
            serviceProvider.GetRequiredService<IPasswordHasher>(),
            serviceProvider.GetRequiredService<ITokenIssuer>(),
            serviceProvider.GetRequiredService<IExternalIdentityVerifier>(),
            serviceProvider.GetRequiredService<IPasswordResetNotificationSender>(),
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.RefreshTokenDays));
        services.AddScoped<ProjectService>();
        services.AddScoped<ProjectActivityService>();
        services.AddScoped<ProjectMemberService>();
        services.AddScoped<ProjectRolePermissionService>();
        services.AddScoped<SprintService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TaskSubmissionService>();
        services.AddScoped<TaskDeadlineService>();
        services.AddScoped<ProjectStorageService>();
        services.AddScoped<AdminAuthorizationService>();
        services.AddScoped<AdminOverviewService>();
        services.AddScoped<AdminAccountService>();
        services.AddScoped<RecoveryAdministrationService>();
        services.AddScoped<SubscriptionPlanService>();
        services.AddScoped<PaymentAdministrationService>();
        services.AddScoped<FeedbackAdministrationService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<GmailLinkService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TaskEmailNotificationService>();
        services.AddScoped<BillingService>();
        services.AddScoped<SubscriptionQuotaService>();
        services.AddScoped<SupportService>();
        services.AddScoped<SystemSettingService>();
        services.AddScoped<SupportConversationAdministrationService>();
        return services;
    }
}
