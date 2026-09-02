using Planora.Domain.Projects;

namespace Planora.Application.Projects;

public sealed record CreateProjectRequest(string Name, string Description, DateTimeOffset? StartAt, DateTimeOffset? EndAt);
public sealed record UpdateProjectRequest(string Name, string Description, DateTimeOffset? StartAt, DateTimeOffset? EndAt, ProjectStatus? Status = null);
public sealed record ProjectResponse(Guid Id, string Name, string Description, ProjectStatus Status, DateTimeOffset? StartAt, DateTimeOffset? EndAt, int MemberCount, DateTimeOffset UpdatedAt);
