using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetTasksAsync(CurrentUser actor, TaskQuery query);
    Task<TaskDto> GetTaskAsync(CurrentUser actor, int id);
    Task<TaskDto> CreateAsync(CurrentUser actor, CreateTaskRequest request);
    Task<TaskDto> UpdateAsync(CurrentUser actor, int id, UpdateTaskRequest request);
    Task<TaskDto> AssignAsync(CurrentUser actor, int id, int? assigneeId);
    Task<TaskDto> UpdateStatusAsync(CurrentUser actor, int id, TaskItemStatus status);
    Task DeleteAsync(CurrentUser actor, int id);
    IQueryable<TaskItem> VisibleTasks(CurrentUser actor);
    Task EnsureCanViewAsync(CurrentUser actor, int taskId);
}

/// <summary>
/// Task business rules:
///  - Admin: full access; can assign tasks to Managers and Users.
///  - Manager: creates tasks and assigns them to members of the teams they manage (or to themselves);
///             sees tasks they created, tasks assigned to them and tasks of their teams.
///  - User: sees and updates the status of tasks assigned to them.
/// </summary>
public class TaskService(AppDbContext db, INotificationService notifications) : ITaskService
{
    private IQueryable<TaskItem> WithDetails(IQueryable<TaskItem> q) => q
        .Include(t => t.CreatedBy)
        .Include(t => t.AssignedTo)
        .Include(t => t.Team)
        .Include(t => t.Comments);

    public IQueryable<TaskItem> VisibleTasks(CurrentUser actor) => actor.Role switch
    {
        UserRole.Admin => db.Tasks,
        UserRole.Manager => db.Tasks.Where(t =>
            t.CreatedById == actor.Id
            || t.AssignedToId == actor.Id
            || (t.Team != null && t.Team.ManagerId == actor.Id)
            || (t.AssignedTo != null && t.AssignedTo.Team != null && t.AssignedTo.Team.ManagerId == actor.Id)),
        _ => db.Tasks.Where(t => t.AssignedToId == actor.Id)
    };

    public async Task<PagedResult<TaskDto>> GetTasksAsync(CurrentUser actor, TaskQuery q)
    {
        var query = VisibleTasks(actor);

        if (q.Status.HasValue) query = query.Where(t => t.Status == q.Status);
        if (q.Priority.HasValue) query = query.Where(t => t.Priority == q.Priority);
        if (q.DueFrom.HasValue) query = query.Where(t => t.DueDate >= q.DueFrom.Value.Date);
        if (q.DueTo.HasValue)
        {
            var endExclusive = q.DueTo.Value.Date.AddDays(1);
            query = query.Where(t => t.DueDate < endExclusive);
        }
        if (q.Overdue == true)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(t => t.Status != TaskItemStatus.Done && t.DueDate < today);
        }
        if (q.AssignedToId.HasValue) query = query.Where(t => t.AssignedToId == q.AssignedToId);
        if (q.TeamId.HasValue) query = query.Where(t => t.TeamId == q.TeamId);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(t => t.Title.Contains(s) || (t.Description != null && t.Description.Contains(s)));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("priority", false) => query.OrderBy(t => t.Priority == TaskPriority.Low ? 0 : t.Priority == TaskPriority.Medium ? 1 : 2),
            ("priority", true) => query.OrderByDescending(t => t.Priority == TaskPriority.Low ? 0 : t.Priority == TaskPriority.Medium ? 1 : 2),
            ("status", false) => query.OrderBy(t => t.Status == TaskItemStatus.ToDo ? 0 : t.Status == TaskItemStatus.InProgress ? 1 : 2),
            ("status", true) => query.OrderByDescending(t => t.Status == TaskItemStatus.ToDo ? 0 : t.Status == TaskItemStatus.InProgress ? 1 : 2),
            ("title", false) => query.OrderBy(t => t.Title),
            ("title", true) => query.OrderByDescending(t => t.Title),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            ("createdat", true) => query.OrderByDescending(t => t.CreatedAt),
            ("duedate", true) => query.OrderByDescending(t => t.DueDate),
            // Default: soonest deadline first, tasks without a deadline last.
            _ => query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
        };

        var total = await query.CountAsync();
        var items = await WithDetails(query)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .AsSplitQuery()
            .ToListAsync();

        return new PagedResult<TaskDto>(items.Select(t => t.ToDto()).ToList(), q.Page, q.PageSize, total);
    }

    public async Task<TaskDto> GetTaskAsync(CurrentUser actor, int id)
    {
        var task = await WithDetails(VisibleTasks(actor)).AsSplitQuery().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
        {
            if (await db.Tasks.AnyAsync(t => t.Id == id)) throw new ForbiddenException("You do not have access to this task.");
            throw new NotFoundException("Task not found.");
        }
        return task.ToDto();
    }

    public async Task EnsureCanViewAsync(CurrentUser actor, int taskId)
    {
        if (await VisibleTasks(actor).AnyAsync(t => t.Id == taskId)) return;
        if (await db.Tasks.AnyAsync(t => t.Id == taskId)) throw new ForbiddenException("You do not have access to this task.");
        throw new NotFoundException("Task not found.");
    }

    public async Task<TaskDto> CreateAsync(CurrentUser actor, CreateTaskRequest request)
    {
        if (actor.Role == UserRole.User) throw new ForbiddenException("Only Admins and Managers can create tasks.");

        var teamId = request.TeamId;
        if (teamId.HasValue) await EnsureCanUseTeam(actor, teamId.Value);

        User? assignee = null;
        if (request.AssignedToId.HasValue)
        {
            assignee = await EnsureCanAssignTo(actor, request.AssignedToId.Value);
            teamId ??= assignee.TeamId;
        }

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate?.Date,
            CreatedById = actor.Id,
            AssignedToId = assignee?.Id,
            TeamId = teamId
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        if (assignee is not null && assignee.Id != actor.Id)
            await NotifyAssigned(task, assignee);

        return await LoadDto(task.Id);
    }

    public async Task<TaskDto> UpdateAsync(CurrentUser actor, int id, UpdateTaskRequest request)
    {
        var task = await LoadForEdit(actor, id);
        if (request.TeamId.HasValue && request.TeamId != task.TeamId)
            await EnsureCanUseTeam(actor, request.TeamId.Value);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.DueDate = request.DueDate?.Date;
        task.TeamId = request.TeamId;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await LoadDto(id);
    }

    public async Task<TaskDto> AssignAsync(CurrentUser actor, int id, int? assigneeId)
    {
        var task = await LoadForEdit(actor, id);
        if (task.AssignedToId == assigneeId) return await LoadDto(id);

        User? assignee = null;
        if (assigneeId.HasValue) assignee = await EnsureCanAssignTo(actor, assigneeId.Value);

        task.AssignedToId = assigneeId;
        task.TeamId ??= assignee?.TeamId;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (assignee is not null && assignee.Id != actor.Id)
            await NotifyAssigned(task, assignee);

        return await LoadDto(id);
    }

    public async Task<TaskDto> UpdateStatusAsync(CurrentUser actor, int id, TaskItemStatus status)
    {
        var task = await VisibleTasks(actor).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
        {
            await EnsureCanViewAsync(actor, id); // throws the right 403/404
            throw new NotFoundException("Task not found.");
        }

        if (actor.Role == UserRole.User && task.AssignedToId != actor.Id)
            throw new ForbiddenException("You can only update tasks assigned to you.");

        if (task.Status == status) return await LoadDto(id);

        var oldStatus = task.Status;
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var actorName = (await db.Users.FindAsync(actor.Id))?.FullName ?? "Someone";
        var message = $"{actorName} changed the status of \"{task.Title}\" from {oldStatus.ToDisplay()} to {status.ToDisplay()}.";

        // Notify the creator and the assignee - except whoever made the change.
        var recipients = new[] { task.CreatedById, task.AssignedToId }
            .Where(uid => uid.HasValue && uid.Value != actor.Id)
            .Select(uid => uid!.Value)
            .Distinct();
        foreach (var uid in recipients)
            await notifications.NotifyAsync(uid, NotificationType.TaskStatusChanged, message, task.Id);

        return await LoadDto(id);
    }

    public async Task DeleteAsync(CurrentUser actor, int id)
    {
        var task = await LoadForEdit(actor, id);
        db.Notifications.RemoveRange(db.Notifications.Where(n => n.TaskItemId == id));
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }

    // ---------- helpers ----------

    /// <summary>Admins can edit any task; Managers can edit tasks they created or that belong to their teams.</summary>
    private async Task<TaskItem> LoadForEdit(CurrentUser actor, int id)
    {
        var task = await db.Tasks.Include(t => t.Team).FirstOrDefaultAsync(t => t.Id == id)
                   ?? throw new NotFoundException("Task not found.");

        var canEdit = actor.Role switch
        {
            UserRole.Admin => true,
            UserRole.Manager => task.CreatedById == actor.Id || task.Team?.ManagerId == actor.Id,
            _ => false
        };
        if (!canEdit) throw new ForbiddenException("You are not allowed to modify this task.");
        return task;
    }

    private async Task<User> EnsureCanAssignTo(CurrentUser actor, int assigneeId)
    {
        var assignee = await db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == assigneeId)
                       ?? throw new BadRequestException("Assignee not found.");

        if (actor.Role == UserRole.Admin) return assignee;

        // Manager: themselves or members of a team they manage.
        if (assignee.Id == actor.Id) return assignee;
        if (assignee.Team?.ManagerId == actor.Id) return assignee;

        throw new ForbiddenException("Managers can only assign tasks to members of their own teams.");
    }

    private async Task EnsureCanUseTeam(CurrentUser actor, int teamId)
    {
        var team = await db.Teams.FindAsync(teamId) ?? throw new BadRequestException("Team not found.");
        if (actor.Role == UserRole.Manager && team.ManagerId != actor.Id)
            throw new ForbiddenException("You can only create tasks for teams you manage.");
    }

    private async Task NotifyAssigned(TaskItem task, User assignee)
    {
        var due = task.DueDate.HasValue ? $" (due {task.DueDate:yyyy-MM-dd})" : string.Empty;
        await notifications.NotifyAsync(assignee.Id, NotificationType.TaskAssigned,
            $"You have been assigned the task \"{task.Title}\"{due}.", task.Id);
    }

    private async Task<TaskDto> LoadDto(int id) =>
        (await WithDetails(db.Tasks).AsSplitQuery().FirstAsync(t => t.Id == id)).ToDto();
}
