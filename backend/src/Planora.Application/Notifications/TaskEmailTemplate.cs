using Planora.Domain.Tasks;

namespace Planora.Application.Notifications;

public enum TaskEmailEvent { Assigned, Updated }

/// <summary>
/// Composes the Vietnamese subject and body for task emails. Kept separate from delivery so
/// the wording can be asserted without a database or SMTP host.
/// </summary>
public static class TaskEmailTemplate
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static string BuildSubject(TaskEmailEvent taskEmailEvent, string taskTitle) => taskEmailEvent switch
    {
        TaskEmailEvent.Assigned => $"[Planora] Bạn được giao công việc \"{taskTitle}\"",
        _ => $"[Planora] Công việc \"{taskTitle}\" đã được cập nhật"
    };

    public static string BuildBody(
        TaskEmailEvent taskEmailEvent,
        ProjectTask projectTask,
        string projectName,
        string actorDisplayName,
        string recipientDisplayName)
    {
        var headline = taskEmailEvent == TaskEmailEvent.Assigned
            ? $"{actorDisplayName} đã giao cho bạn một công việc trong project {projectName}."
            : $"{actorDisplayName} đã cập nhật một công việc bạn đang phụ trách trong project {projectName}.";
        var deadline = projectTask.EffectiveDueAt is DateTimeOffset dueAt
            ? $"{dueAt.ToOffset(VietnamOffset):dd/MM/yyyy HH:mm} (giờ Việt Nam)"
            : "Chưa đặt";
        var description = string.IsNullOrWhiteSpace(projectTask.Description) ? "Chưa có mô tả." : projectTask.Description;

        return $"""
            Xin chào {recipientDisplayName},

            {headline}

            Công việc: {projectTask.Title}
            Mức ưu tiên: {projectTask.Priority}
            Trạng thái: {projectTask.Status}
            Deadline: {deadline}
            Loại: {projectTask.Type}

            {description}

            Mở Planora để xem chi tiết và cập nhật tiến độ.

            Trả lời email này nếu bạn cần hỏi thêm {actorDisplayName}.
            Bạn có thể tắt email này trong Planora tại Tài khoản → Giao diện & ngôn ngữ.
            """;
    }
}
