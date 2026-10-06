using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetAsync(CurrentUser actor, int taskId);
    Task<CommentDto> AddAsync(CurrentUser actor, int taskId, string content);
    Task DeleteAsync(CurrentUser actor, int taskId, int commentId);
}

/// <summary>Anyone who can see a task can read and add comments on it.</summary>
public class CommentService(AppDbContext db, ITaskService tasks) : ICommentService
{
    public async Task<List<CommentDto>> GetAsync(CurrentUser actor, int taskId)
    {
        await tasks.EnsureCanViewAsync(actor, taskId);
        var comments = await db.Comments.Include(c => c.Author)
            .Where(c => c.TaskItemId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        return comments.Select(c => c.ToDto()).ToList();
    }

    public async Task<CommentDto> AddAsync(CurrentUser actor, int taskId, string content)
    {
        await tasks.EnsureCanViewAsync(actor, taskId);
        var comment = new Comment { TaskItemId = taskId, AuthorId = actor.Id, Content = content.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        await db.Entry(comment).Reference(c => c.Author).LoadAsync();
        return comment.ToDto();
    }

    /// <summary>Authors can delete their own comments; Admins can delete any comment.</summary>
    public async Task DeleteAsync(CurrentUser actor, int taskId, int commentId)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId)
                      ?? throw new NotFoundException("Comment not found.");
        if (comment.AuthorId != actor.Id && actor.Role != UserRole.Admin)
            throw new ForbiddenException("You can only delete your own comments.");

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
    }
}
