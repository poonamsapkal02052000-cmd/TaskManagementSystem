using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

/// <summary>
/// Seeds demo data (sample credentials are listed in README.md).
/// Runs only when the database has no users.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password@123";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        var admin = new User { FullName = "System Admin", Email = "admin@taskmanager.com", PasswordHash = hash, Role = UserRole.Admin };
        var manager = new User { FullName = "Maya Manager", Email = "manager@taskmanager.com", PasswordHash = hash, Role = UserRole.Manager };
        var manager2 = new User { FullName = "Mark Manager", Email = "manager2@taskmanager.com", PasswordHash = hash, Role = UserRole.Manager };
        var user1 = new User { FullName = "Uma User", Email = "user@taskmanager.com", PasswordHash = hash, Role = UserRole.User };
        var user2 = new User { FullName = "Ravi Kumar", Email = "ravi@taskmanager.com", PasswordHash = hash, Role = UserRole.User };
        var user3 = new User { FullName = "Sara Lee", Email = "sara@taskmanager.com", PasswordHash = hash, Role = UserRole.User };

        db.Users.AddRange(admin, manager, manager2, user1, user2, user3);
        await db.SaveChangesAsync();

        var engineering = new Team { Name = "Engineering", Description = "Product development team", ManagerId = manager.Id };
        var marketing = new Team { Name = "Marketing", Description = "Growth and campaigns", ManagerId = manager2.Id };
        db.Teams.AddRange(engineering, marketing);
        await db.SaveChangesAsync();

        user1.TeamId = engineering.Id;
        user2.TeamId = engineering.Id;
        user3.TeamId = marketing.Id;
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var tasks = new List<TaskItem>
        {
            new() { Title = "Set up CI pipeline", Description = "Configure GitHub Actions to build and test.", Priority = TaskPriority.High, Status = TaskItemStatus.InProgress, DueDate = today.AddDays(2), CreatedById = manager.Id, AssignedToId = user1.Id, TeamId = engineering.Id },
            new() { Title = "Design login page", Description = "Responsive and accessible login UI.", Priority = TaskPriority.Medium, Status = TaskItemStatus.Done, DueDate = today.AddDays(-3), CreatedById = manager.Id, AssignedToId = user1.Id, TeamId = engineering.Id },
            new() { Title = "Write API documentation", Description = "Document endpoints with Swagger.", Priority = TaskPriority.Low, Status = TaskItemStatus.ToDo, DueDate = today.AddDays(7), CreatedById = manager.Id, AssignedToId = user2.Id, TeamId = engineering.Id },
            new() { Title = "Fix overdue bug #42", Description = "Null reference on task details page.", Priority = TaskPriority.High, Status = TaskItemStatus.ToDo, DueDate = today.AddDays(-1), CreatedById = manager.Id, AssignedToId = user2.Id, TeamId = engineering.Id },
            new() { Title = "Plan Q4 campaign", Description = "Draft social media plan.", Priority = TaskPriority.Medium, Status = TaskItemStatus.InProgress, DueDate = today.AddDays(10), CreatedById = manager2.Id, AssignedToId = user3.Id, TeamId = marketing.Id },
            new() { Title = "Quarterly review with managers", Description = "Review team performance.", Priority = TaskPriority.High, Status = TaskItemStatus.ToDo, DueDate = today.AddDays(5), CreatedById = admin.Id, AssignedToId = manager.Id }
        };
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();

        db.Comments.AddRange(
            new Comment { TaskItemId = tasks[0].Id, AuthorId = manager.Id, Content = "Please include the test step as well." },
            new Comment { TaskItemId = tasks[0].Id, AuthorId = user1.Id, Content = "Sure, working on it now." });
        await db.SaveChangesAsync();
    }
}
