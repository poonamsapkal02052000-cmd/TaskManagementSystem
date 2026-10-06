using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs;

// ---------- Auth ----------
public record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter and a digit.")]
    string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

// ---------- Users ----------
public record UserDto(int Id, string FullName, string Email, UserRole Role, int? TeamId, string? TeamName);

public record UpdateUserRoleRequest([Required, EnumDataType(typeof(UserRole))] UserRole Role);

public record CreateUserRequest(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Required, EnumDataType(typeof(UserRole))] UserRole Role);

// ---------- Teams ----------
public record TeamDto(int Id, string Name, string? Description, int? ManagerId, string? ManagerName, List<UserDto> Members);

public record CreateTeamRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    int? ManagerId);

public record UpdateTeamRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    int? ManagerId);

public record AddTeamMemberRequest([Required, Range(1, int.MaxValue)] int UserId);

// ---------- Tasks ----------
public record TaskDto(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? DueDate,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CreatedById,
    string CreatedByName,
    int? AssignedToId,
    string? AssignedToName,
    int? TeamId,
    string? TeamName,
    int CommentCount);

public record CreateTaskRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Title,
    [StringLength(4000)] string? Description,
    [EnumDataType(typeof(TaskPriority))] TaskPriority Priority,
    DateTime? DueDate,
    int? AssignedToId,
    int? TeamId);

public record UpdateTaskRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Title,
    [StringLength(4000)] string? Description,
    [EnumDataType(typeof(TaskPriority))] TaskPriority Priority,
    DateTime? DueDate,
    int? TeamId);

public record AssignTaskRequest(int? AssignedToId);

public record UpdateTaskStatusRequest([Required, EnumDataType(typeof(TaskItemStatus))] TaskItemStatus Status);

public class TaskQuery
{
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    /// <summary>Tasks due on or after this date.</summary>
    public DateTime? DueFrom { get; set; }
    /// <summary>Tasks due on or before this date.</summary>
    public DateTime? DueTo { get; set; }
    public bool? Overdue { get; set; }
    public int? AssignedToId { get; set; }
    public int? TeamId { get; set; }
    [StringLength(100)]
    public string? Search { get; set; }
    /// <summary>dueDate | priority | status | createdAt | title</summary>
    public string? SortBy { get; set; }
    public bool Desc { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// ---------- Comments ----------
public record CommentDto(int Id, string Content, DateTime CreatedAt, int AuthorId, string AuthorName);

public record CreateCommentRequest([Required, StringLength(2000, MinimumLength = 1)] string Content);

// ---------- Notifications ----------
public record NotificationDto(int Id, NotificationType Type, string Message, bool IsRead, DateTime CreatedAt, int? TaskItemId);

// ---------- Dashboard ----------
public record StatusCounts(int ToDo, int InProgress, int Done, int Total);

public record UserTaskSummary(int UserId, string FullName, StatusCounts Counts, int Overdue);

public record DashboardDto(
    StatusCounts Totals,
    int Overdue,
    int DueThisWeek,
    Dictionary<string, int> ByPriority,
    List<UserTaskSummary> PerUser,
    List<TaskDto> UpcomingDeadlines);
