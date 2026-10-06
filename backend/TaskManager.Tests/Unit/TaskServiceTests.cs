using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Tests.Unit;

public class TaskServiceTests : IDisposable
{
    private readonly TestDb _t = new();

    private CurrentUser As(User u) => new(u.Id, u.Role);

    private static CreateTaskRequest NewTask(int? assignee = null, int? team = null, DateTime? due = null) =>
        new("Write tests", "desc", TaskPriority.High, due, assignee, team);

    [Fact]
    public async Task Manager_can_create_task_for_team_member_and_member_is_notified()
    {
        var dto = await _t.Tasks().CreateAsync(As(_t.Manager), NewTask(_t.Member.Id));

        Assert.Equal(_t.Member.Id, dto.AssignedToId);
        Assert.Equal(_t.Team.Id, dto.TeamId); // inherited from assignee
        Assert.Equal(TaskItemStatus.ToDo, dto.Status);

        var note = await _t.Db.Notifications.SingleAsync();
        Assert.Equal(_t.Member.Id, note.UserId);
        Assert.Equal(NotificationType.TaskAssigned, note.Type);
        Assert.Single(_t.Email.Sent);
        Assert.Equal(_t.Member.Email, _t.Email.Sent[0].To);
    }

    [Fact]
    public async Task Manager_cannot_assign_task_outside_their_team()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _t.Tasks().CreateAsync(As(_t.Manager), NewTask(_t.Outsider.Id)));
    }

    [Fact]
    public async Task Manager_cannot_create_task_for_another_managers_team()
    {
        var otherTeam = new Team { Name = "Other", ManagerId = _t.OtherManager.Id };
        _t.Db.Teams.Add(otherTeam);
        await _t.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _t.Tasks().CreateAsync(As(_t.Manager), NewTask(team: otherTeam.Id)));
    }

    [Fact]
    public async Task Admin_can_assign_task_to_a_manager()
    {
        var dto = await _t.Tasks().CreateAsync(As(_t.Admin), NewTask(_t.OtherManager.Id));
        Assert.Equal(_t.OtherManager.Id, dto.AssignedToId);
    }

    [Fact]
    public async Task User_cannot_create_tasks()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _t.Tasks().CreateAsync(As(_t.Member), NewTask()));
    }

    [Fact]
    public async Task User_sees_only_tasks_assigned_to_them()
    {
        var svc = _t.Tasks();
        await svc.CreateAsync(As(_t.Admin), NewTask(_t.Member.Id));
        await svc.CreateAsync(As(_t.Admin), NewTask(_t.Outsider.Id));
        await svc.CreateAsync(As(_t.Admin), NewTask());

        var result = await svc.GetTasksAsync(As(_t.Member), new TaskQuery());

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(_t.Member.Id, t.AssignedToId));
    }

    [Fact]
    public async Task Assignee_can_update_status_and_creator_is_notified()
    {
        var svc = _t.Tasks();
        var task = await svc.CreateAsync(As(_t.Manager), NewTask(_t.Member.Id));

        var updated = await svc.UpdateStatusAsync(As(_t.Member), task.Id, TaskItemStatus.InProgress);

        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
        var statusNote = await _t.Db.Notifications.SingleAsync(n => n.Type == NotificationType.TaskStatusChanged);
        Assert.Equal(_t.Manager.Id, statusNote.UserId);
        Assert.Contains("In Progress", statusNote.Message);
    }

    [Fact]
    public async Task User_cannot_update_status_of_someone_elses_task()
    {
        var svc = _t.Tasks();
        var task = await svc.CreateAsync(As(_t.Admin), NewTask(_t.Outsider.Id));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.UpdateStatusAsync(As(_t.Member), task.Id, TaskItemStatus.Done));
    }

    [Fact]
    public async Task Filters_by_status_priority_and_deadline()
    {
        var svc = _t.Tasks();
        var today = DateTime.UtcNow.Date;
        await svc.CreateAsync(As(_t.Admin), new CreateTaskRequest("Low soon", null, TaskPriority.Low, today.AddDays(1), null, null));
        await svc.CreateAsync(As(_t.Admin), new CreateTaskRequest("High soon", null, TaskPriority.High, today.AddDays(2), null, null));
        var late = await svc.CreateAsync(As(_t.Admin), new CreateTaskRequest("High later", null, TaskPriority.High, today.AddDays(30), null, null));
        await svc.UpdateStatusAsync(As(_t.Admin), late.Id, TaskItemStatus.Done);

        var high = await svc.GetTasksAsync(As(_t.Admin), new TaskQuery { Priority = TaskPriority.High });
        Assert.Equal(2, high.TotalCount);

        var thisWeek = await svc.GetTasksAsync(As(_t.Admin), new TaskQuery { DueTo = today.AddDays(7) });
        Assert.Equal(2, thisWeek.TotalCount);

        var done = await svc.GetTasksAsync(As(_t.Admin), new TaskQuery { Status = TaskItemStatus.Done });
        Assert.Equal("High later", Assert.Single(done.Items).Title);
    }

    [Fact]
    public async Task Overdue_flag_is_set_for_past_due_open_tasks()
    {
        var dto = await _t.Tasks().CreateAsync(As(_t.Admin), NewTask(due: DateTime.UtcNow.Date.AddDays(-2)));
        Assert.True(dto.IsOverdue);
    }

    [Fact]
    public async Task Other_manager_cannot_edit_or_delete_task()
    {
        var svc = _t.Tasks();
        var task = await svc.CreateAsync(As(_t.Manager), NewTask(_t.Member.Id));

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.DeleteAsync(As(_t.OtherManager), task.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.UpdateAsync(As(_t.OtherManager), task.Id, new UpdateTaskRequest("New title", null, TaskPriority.Low, null, null)));
    }

    public void Dispose() => _t.Dispose();
}
