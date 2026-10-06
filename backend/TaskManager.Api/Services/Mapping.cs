using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public static class Mapping
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.FullName, u.Email, u.Role, u.TeamId, u.Team?.Name);

    public static TeamDto ToDto(this Team t) =>
        new(t.Id, t.Name, t.Description, t.ManagerId, t.Manager?.FullName,
            t.Members.OrderBy(m => m.FullName).Select(m => m.ToDto()).ToList());

    public static TaskDto ToDto(this TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
            IsOverdue(t), t.CreatedAt, t.UpdatedAt,
            t.CreatedById, t.CreatedBy?.FullName ?? string.Empty,
            t.AssignedToId, t.AssignedTo?.FullName,
            t.TeamId, t.Team?.Name,
            t.Comments.Count);

    public static CommentDto ToDto(this Comment c) =>
        new(c.Id, c.Content, c.CreatedAt, c.AuthorId, c.Author?.FullName ?? string.Empty);

    public static NotificationDto ToDto(this Notification n) =>
        new(n.Id, n.Type, n.Message, n.IsRead, n.CreatedAt, n.TaskItemId);

    public static bool IsOverdue(TaskItem t) =>
        t.Status != TaskItemStatus.Done && t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date;

    public static string ToDisplay(this TaskItemStatus s) => s switch
    {
        TaskItemStatus.ToDo => "To Do",
        TaskItemStatus.InProgress => "In Progress",
        TaskItemStatus.Done => "Done",
        _ => s.ToString()
    };
}
