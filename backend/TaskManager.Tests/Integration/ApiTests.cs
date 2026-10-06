using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Tests.Integration;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Admin = "admin@taskmanager.com";
    private const string Manager = "manager@taskmanager.com";
    private const string User = "user@taskmanager.com";

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var res = await factory.CreateClient().GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(Admin, "wrong"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Register_then_login_and_get_profile()
    {
        var client = factory.CreateClient();
        var email = $"new{Guid.NewGuid():N}@test.com";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("New Person", email, "Secret123"));
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>(ApiFactory.Json);
        Assert.Equal(UserRole.User, auth!.User.Role);

        var dup = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Dup", email, "Secret123"));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Weak", "weak@test.com", "short"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task User_role_cannot_create_tasks_or_list_users()
    {
        var client = await factory.ClientAsAsync(User);

        var create = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Nope", null, TaskPriority.Low, null, null, null), ApiFactory.Json);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var users = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task Manager_creates_task_user_updates_status_and_comments()
    {
        var manager = await factory.ClientAsAsync(Manager);
        var user = await factory.ClientAsAsync(User);
        var me = await (await user.GetAsync("/api/auth/me")).Content.ReadFromJsonAsync<UserDto>(ApiFactory.Json);

        var createRes = await manager.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Integration task", "From test", TaskPriority.High, DateTime.UtcNow.AddDays(3), me!.Id, null),
            ApiFactory.Json);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var task = await createRes.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json);

        // User was notified about the assignment
        var notes = await user.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", ApiFactory.Json);
        Assert.Contains(notes!, n => n.TaskItemId == task!.Id && n.Type == NotificationType.TaskAssigned);

        // User moves it to Done
        var statusRes = await user.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status",
            new UpdateTaskStatusRequest(TaskItemStatus.Done), ApiFactory.Json);
        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);

        // User comments
        var commentRes = await user.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new CreateCommentRequest("Finished!"));
        Assert.Equal(HttpStatusCode.Created, commentRes.StatusCode);

        // Manager sees the comment and a status-change notification
        var comments = await manager.GetFromJsonAsync<List<CommentDto>>($"/api/tasks/{task.Id}/comments", ApiFactory.Json);
        Assert.Contains(comments!, c => c.Content == "Finished!");
        var managerNotes = await manager.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", ApiFactory.Json);
        Assert.Contains(managerNotes!, n => n.TaskItemId == task.Id && n.Type == NotificationType.TaskStatusChanged);
    }

    [Fact]
    public async Task Tasks_can_be_filtered_by_status()
    {
        var admin = await factory.ClientAsAsync(Admin);
        var result = await admin.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks?status=InProgress", ApiFactory.Json);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, t => Assert.Equal(TaskItemStatus.InProgress, t.Status));
    }

    [Fact]
    public async Task Dashboard_returns_status_overview()
    {
        var admin = await factory.ClientAsAsync(Admin);
        var dash = await admin.GetFromJsonAsync<DashboardDto>("/api/dashboard", ApiFactory.Json);
        Assert.True(dash!.Totals.Total > 0);
        Assert.Equal(dash.Totals.Total, dash.Totals.ToDo + dash.Totals.InProgress + dash.Totals.Done);
        Assert.NotEmpty(dash.PerUser);
    }

    [Fact]
    public async Task Only_admin_can_create_teams()
    {
        var manager = await factory.ClientAsAsync(Manager);
        var res = await manager.PostAsJsonAsync("/api/teams", new CreateTeamRequest("Sneaky", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var admin = await factory.ClientAsAsync(Admin);
        var ok = await admin.PostAsJsonAsync("/api/teams", new CreateTeamRequest($"Team {Guid.NewGuid():N}", "desc", null));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_available()
    {
        var res = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
