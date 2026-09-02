using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Users;

namespace Planora.Application.Administration;

public sealed class AdminAuthorizationService(IPlanoraDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<ApplicationError?> GetSystemAdministratorAuthorizationErrorAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();

        var isSystemAdministrator = await dbContext.Users.AnyAsync(
            user => user.Id == currentUser.UserId &&
                    user.Status == UserStatus.Active &&
                    user.SystemRole == SystemRole.SystemAdministrator,
            cancellationToken);

        return isSystemAdministrator
            ? null
            : ApplicationErrors.Forbidden("admin.access_denied", "System administrator access is required.");
    }
}
