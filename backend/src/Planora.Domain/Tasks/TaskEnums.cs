namespace Planora.Domain.Tasks;

public enum PlanoraTaskStatus { Todo, InProgress, Submitted, Rework, Done, Expired, Cancelled }
public enum TaskPriority { Low, Medium, High, Urgent }
public enum SubmissionStatus { PendingReview, Approved, ReworkRequested }
public enum ExtensionRequestStatus { Pending, Approved, Rejected }
public enum DeadlineChangeType { MemberRequestApproved, LeaderDirect }
public enum ProjectTaskType { General, Documentation, BugFix, Feature, Testing, Research, Design, Meeting }
public enum SubmissionRequirement { Any, LinkOnly, FileOnly, Word, Excel, Pdf, PowerPoint, Image }
