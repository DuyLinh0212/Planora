using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Support;

namespace Planora.Application.Administration;

public sealed class FeedbackAdministrationService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<PagedResponse<FeedbackItemResponse>>> GetFeedbackItemsAsync(
        FeedbackStatus? status,
        FeedbackPriority? priority,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<PagedResponse<FeedbackItemResponse>>(authorizationError);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.FeedbackItems.AsQueryable();
        if (status is not null)
            query = query.Where(feedback => feedback.Status == status);
        if (priority is not null)
            query = query.Where(feedback => feedback.Priority == priority);

        var totalCount = await query.CountAsync(cancellationToken);
        var feedbackItems = await ProjectFeedbackItems(query)
            .OrderByDescending(feedback => feedback.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success(new PagedResponse<FeedbackItemResponse>(feedbackItems, totalCount, page, pageSize));
    }

    public async Task<ApplicationResult<FeedbackItemResponse>> GetFeedbackItemByIdAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<FeedbackItemResponse>(authorizationError);

        var feedback = await ProjectFeedbackItems(dbContext.FeedbackItems.Where(candidate => candidate.Id == feedbackId))
            .FirstOrDefaultAsync(cancellationToken);
        return feedback is null
            ? ApplicationResult.Failure<FeedbackItemResponse>(ApplicationErrors.NotFound("Feedback"))
            : ApplicationResult.Success(feedback);
    }

    public async Task<ApplicationResult> AssignFeedbackItemAsync(
        Guid feedbackId,
        AssignFeedbackItemRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var assigneeIsAdministrator = await dbContext.Users.AnyAsync(
            user => user.Id == request.AdministratorUserId && user.SystemRole == Domain.Users.SystemRole.SystemAdministrator,
            cancellationToken);
        if (!assigneeIsAdministrator)
            return ApplicationResult.Failure(ApplicationErrors.Validation("feedback.assignee_invalid", "Feedback can only be assigned to a system administrator.", "administratorUserId"));

        var feedback = await dbContext.FeedbackItems.FindAsync([feedbackId], cancellationToken);
        if (feedback is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Feedback"));

        feedback.AssignFeedbackToAdministrator(request.AdministratorUserId);
        AddFeedbackAuditLog("feedback.assigned", feedback.Id, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> ResolveFeedbackItemAsync(
        Guid feedbackId,
        ResolveFeedbackItemRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var feedback = await dbContext.FeedbackItems.FindAsync([feedbackId], cancellationToken);
        if (feedback is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Feedback"));

        var currentTime = timeProvider.GetUtcNow();
        feedback.ResolveFeedback(request.InternalNote, currentTime);
        AddFeedbackAuditLog("feedback.resolved", feedback.Id, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private IQueryable<FeedbackItemResponse> ProjectFeedbackItems(IQueryable<Feedback> query) =>
        query.Select(feedback => new FeedbackItemResponse(
            feedback.Id,
            feedback.UserId,
            dbContext.Users.Where(user => user.Id == feedback.UserId).Select(user => user.Email).FirstOrDefault() ?? "Anonymous",
            feedback.Category,
            feedback.Subject,
            feedback.Content,
            feedback.Status,
            feedback.Priority,
            feedback.AssignedAdminUserId,
            feedback.InternalNote,
            feedback.CreatedAt,
            feedback.ResolvedAt));

    private void AddFeedbackAuditLog(string action, Guid feedbackId, DateTimeOffset createdAt) =>
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(
            currentUser.UserId,
            null,
            action,
            nameof(Feedback),
            feedbackId.ToString(),
            null,
            null,
            currentUser.IpAddress,
            createdAt));
}
