using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Tests.Unit;

/// <summary>Builds an isolated in-memory database with a small, known data set.</summary>
public sealed class TestDb : IDisposable
{
    public AppDbContext Db { get; }
    public User Admin { get; }
    public User Manager { get; }
    public User OtherManager { get; }
    public User Member { get; }
    public User Outsider { get; }
    public Team Team { get; }
    public FakeEmailSender Email { get; } = new();

    public TestDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new AppDbContext(options);

        Admin = new User { FullName = "Admin", Email = "a@t.com", Role = UserRole.Admin };
        Manager = new User { FullName = "Manager", Email = "m@t.com", Role = UserRole.Manager };
        OtherManager = new User { FullName = "Other Manager", Email = "om@t.com", Role = UserRole.Manager };
        Member = new User { FullName = "Member", Email = "u@t.com", Role = UserRole.User };
        Outsider = new User { FullName = "Outsider", Email = "o@t.com", Role = UserRole.User };
        Db.Users.AddRange(Admin, Manager, OtherManager, Member, Outsider);
        Db.SaveChanges();

        Team = new Team { Name = "Core", ManagerId = Manager.Id };
        Db.Teams.Add(Team);
        Db.SaveChanges();

        Member.TeamId = Team.Id;
        Db.SaveChanges();
    }

    public NotificationService Notifications() =>
        new(Db, Email, Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);

    public TaskService Tasks() => new(Db, Notifications());

    public void Dispose() => Db.Dispose();
}

public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string body)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
