using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

// ---------- Email ----------
public class EmailSettings
{
    public const string SectionName = "Email";
    /// <summary>When false (default) emails are mocked and written to the log.</summary>
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@taskmanager.com";
    public bool EnableSsl { get; set; } = true;
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

/// <summary>Mock sender used when SMTP is not configured - logs the email instead of sending it.</summary>
public class MockEmailSender(ILogger<MockEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation("[MOCK EMAIL] To: {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _s = options.Value;

    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_s.SmtpHost, _s.SmtpPort) { EnableSsl = _s.EnableSsl };
        if (!string.IsNullOrEmpty(_s.Username))
            client.Credentials = new NetworkCredential(_s.Username, _s.Password);

        using var message = new MailMessage(_s.From, to, subject, body);
        await client.SendMailAsync(message);
    }
}

// ---------- In-app notifications ----------
public interface INotificationService
{
    Task NotifyAsync(int userId, NotificationType type, string message, int? taskId);
    Task<List<NotificationDto>> GetForUserAsync(int userId, bool unreadOnly);
    Task<int> UnreadCountAsync(int userId);
    Task MarkReadAsync(int userId, int notificationId);
    Task MarkAllReadAsync(int userId);
}

public class NotificationService(AppDbContext db, IEmailSender email, ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(int userId, NotificationType type, string message, int? taskId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return;

        db.Notifications.Add(new Notification { UserId = userId, Type = type, Message = message, TaskItemId = taskId });
        await db.SaveChangesAsync();

        // Email failures must never break the main operation.
        try
        {
            var subject = type == NotificationType.TaskAssigned ? "New task assigned" : "Task status updated";
            await email.SendAsync(user.Email, $"[Task Manager] {subject}", message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email notification to {Email}", user.Email);
        }
    }

    public async Task<List<NotificationDto>> GetForUserAsync(int userId, bool unreadOnly)
    {
        var query = db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        var items = await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
        return items.Select(n => n.ToDto()).ToList();
    }

    public Task<int> UnreadCountAsync(int userId) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkReadAsync(int userId, int notificationId)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId)
                ?? throw new NotFoundException("Notification not found.");
        n.IsRead = true;
        await db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        unread.ForEach(n => n.IsRead = true);
        await db.SaveChangesAsync();
    }
}
