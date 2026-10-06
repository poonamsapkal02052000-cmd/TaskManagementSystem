using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CurrentUser actor, TaskQuery filter);
}

/// <summary>Builds the dashboard from the tasks visible to the current user, honouring the same filters as the task list.</summary>
public class DashboardService(ITaskService tasks) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CurrentUser actor, TaskQuery filter)
    {
        var query = tasks.VisibleTasks(actor);
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status);
        if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority);
        if (filter.DueFrom.HasValue) query = query.Where(t => t.DueDate >= filter.DueFrom.Value.Date);
        if (filter.DueTo.HasValue)
        {
            var endExclusive = filter.DueTo.Value.Date.AddDays(1);
            query = query.Where(t => t.DueDate < endExclusive);
        }
        if (filter.TeamId.HasValue) query = query.Where(t => t.TeamId == filter.TeamId);

        var list = await query
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments)
            .AsSplitQuery()
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);

        var perUser = list
            .Where(t => t.AssignedTo != null)
            .GroupBy(t => t.AssignedTo!)
            .Select(g => new UserTaskSummary(g.Key.Id, g.Key.FullName, Count(g), g.Count(Mapping.IsOverdue)))
            .OrderBy(u => u.FullName)
            .ToList();

        var upcoming = list
            .Where(t => t.Status != TaskItemStatus.Done && t.DueDate.HasValue)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => t.ToDto())
            .ToList();

        return new DashboardDto(
            Totals: Count(list),
            Overdue: list.Count(Mapping.IsOverdue),
            DueThisWeek: list.Count(t => t.Status != TaskItemStatus.Done && t.DueDate >= today && t.DueDate <= weekEnd),
            ByPriority: Enum.GetValues<TaskPriority>().ToDictionary(p => p.ToString(), p => list.Count(t => t.Priority == p)),
            PerUser: perUser,
            UpcomingDeadlines: upcoming);
    }

    private static StatusCounts Count(IEnumerable<TaskItem> items)
    {
        var arr = items as ICollection<TaskItem> ?? items.ToList();
        return new StatusCounts(
            arr.Count(t => t.Status == TaskItemStatus.ToDo),
            arr.Count(t => t.Status == TaskItemStatus.InProgress),
            arr.Count(t => t.Status == TaskItemStatus.Done),
            arr.Count);
    }
}
