using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Administration;
using Planora.Domain.Projects;

namespace Planora.Application.Administration;

public sealed record MaintenanceStatusResponse(bool IsEnabled, string Message, DateTimeOffset? UpdatedAt);
public sealed record UpdateMaintenanceStatusRequest(bool IsEnabled, string Message);

public sealed class SystemSettingService(IPlanoraDbContext dbContext, ICurrentUser currentUser, AdminAuthorizationService authorizationService, TimeProvider timeProvider)
{
    private const string MaintenanceKey = "MAINTENANCE";

    public async Task<ApplicationResult<MaintenanceStatusResponse>> GetMaintenanceStatusAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.SystemSettings.FirstOrDefaultAsync(item => item.Key == MaintenanceKey, cancellationToken);
        if (setting is null)
            return ApplicationResult.Success(new MaintenanceStatusResponse(false, "Planora is operating normally.", null));
        var parts = setting.Value.Split('|', 2);
        return ApplicationResult.Success(new MaintenanceStatusResponse(parts[0] == "1", parts.Length > 1 ? parts[1] : "Planora is under maintenance. Please return later.", setting.UpdatedAt));
    }

    public async Task<ApplicationResult> UpdateMaintenanceStatusAsync(UpdateMaintenanceStatusRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await authorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (request.IsEnabled && string.IsNullOrWhiteSpace(request.Message))
            return ApplicationResult.Failure(ApplicationErrors.Validation("maintenance.message_required", "Provide a maintenance message before enabling maintenance mode.", "message"));

        var now = timeProvider.GetUtcNow();
        var value = $"{(request.IsEnabled ? 1 : 0)}|{request.Message.Trim()}";
        var setting = await dbContext.SystemSettings.FirstOrDefaultAsync(item => item.Key == MaintenanceKey, cancellationToken);
        if (setting is null)
        {
            setting = SystemSetting.CreateSystemSetting(MaintenanceKey, value, currentUser.UserId, now);
            dbContext.SystemSettings.Add(setting);
        }
        else
        {
            setting.UpdateSystemSetting(value, currentUser.UserId, now);
        }
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, null, request.IsEnabled ? "maintenance.enabled" : "maintenance.disabled", nameof(SystemSetting), setting.Id.ToString(), null, null, currentUser.IpAddress, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
