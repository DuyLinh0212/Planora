using Planora.Application.Notifications;
using Planora.Domain.Tasks;

namespace Planora.UnitTests;

public sealed class TaskEmailTemplateTests
{
    private static readonly Guid ProjectId = Guid.CreateVersion7();
    private static readonly Guid ActorUserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset CurrentTime = new(2026, 8, 31, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildSubject_ForAssignedEvent_NamesTheTask()
    {
        var subject = TaskEmailTemplate.BuildSubject(TaskEmailEvent.Assigned, "Thiết kế ERD");

        Assert.Equal("[Planora] Bạn được giao công việc \"Thiết kế ERD\"", subject);
    }

    [Fact]
    public void BuildSubject_ForUpdatedEvent_SignalsAnUpdate()
    {
        var subject = TaskEmailTemplate.BuildSubject(TaskEmailEvent.Updated, "Thiết kế ERD");

        Assert.Equal("[Planora] Công việc \"Thiết kế ERD\" đã được cập nhật", subject);
    }

    [Fact]
    public void BuildBody_RendersDeadlineInVietnamTimeAndInvitesAReply()
    {
        var projectTask = CreateProjectTask(new DateTimeOffset(2026, 9, 2, 10, 30, 0, TimeSpan.Zero));

        var body = TaskEmailTemplate.BuildBody(TaskEmailEvent.Assigned, projectTask, "Planora Core", "Nguyễn A", "Trần B");

        Assert.Contains("Xin chào Trần B", body, StringComparison.Ordinal);
        Assert.Contains("Nguyễn A đã giao cho bạn một công việc trong project Planora Core.", body, StringComparison.Ordinal);
        Assert.Contains("02/09/2026 17:30 (giờ Việt Nam)", body, StringComparison.Ordinal);
        Assert.Contains("Trả lời email này nếu bạn cần hỏi thêm Nguyễn A.", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBody_WithoutDeadlineOrDescription_UsesReadableFallbacks()
    {
        var projectTask = CreateProjectTask(null, description: "   ");

        var body = TaskEmailTemplate.BuildBody(TaskEmailEvent.Updated, projectTask, "Planora Core", "Nguyễn A", "Trần B");

        Assert.Contains("Deadline: Chưa đặt", body, StringComparison.Ordinal);
        Assert.Contains("Chưa có mô tả.", body, StringComparison.Ordinal);
        Assert.Contains("đã cập nhật một công việc", body, StringComparison.Ordinal);
    }

    private static ProjectTask CreateProjectTask(DateTimeOffset? dueAt, string description = "Chuẩn hoá lược đồ dữ liệu.") =>
        ProjectTask.CreateProjectTask(ProjectId, null, "Thiết kế ERD", description, TaskPriority.High, dueAt, ActorUserId, CurrentTime);
}
