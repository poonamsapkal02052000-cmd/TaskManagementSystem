using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(CurrentUser actor, UserRole? role, bool unassignedOnly);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateRoleAsync(CurrentUser actor, int userId, UserRole role);
    Task DeleteUserAsync(CurrentUser actor, int userId);
}

public class UserService(AppDbContext db) : IUserService
{
    /// <summary>
    /// Admins see everyone. Managers see non-admin users (so they can build teams and assign tasks).
    /// </summary>
    public async Task<List<UserDto>> GetUsersAsync(CurrentUser actor, UserRole? role, bool unassignedOnly)
    {
        if (actor.Role == UserRole.User) throw new ForbiddenException();

        var query = db.Users.Include(u => u.Team).AsQueryable();
        if (actor.Role == UserRole.Manager) query = query.Where(u => u.Role != UserRole.Admin);
        if (role.HasValue) query = query.Where(u => u.Role == role.Value);
        if (unassignedOnly) query = query.Where(u => u.TeamId == null);

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.ToDto();
    }

    public async Task<UserDto> UpdateRoleAsync(CurrentUser actor, int userId, UserRole role)
    {
        if (actor.Id == userId) throw new BadRequestException("You cannot change your own role.");

        var user = await db.Users.Include(u => u.Team).Include(u => u.ManagedTeams)
                       .FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new NotFoundException("User not found.");

        // A demoted manager no longer manages any team.
        if (user.Role == UserRole.Manager && role != UserRole.Manager)
            foreach (var team in user.ManagedTeams) team.ManagerId = null;

        user.Role = role;
        await db.SaveChangesAsync();
        return user.ToDto();
    }

    public async Task DeleteUserAsync(CurrentUser actor, int userId)
    {
        if (actor.Id == userId) throw new BadRequestException("You cannot delete your own account.");

        var user = await db.Users.Include(u => u.ManagedTeams).FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new NotFoundException("User not found.");

        if (await db.Tasks.AnyAsync(t => t.CreatedById == userId))
            throw new ConflictException("This user created tasks. Reassign or delete those tasks first.");

        foreach (var team in user.ManagedTeams) team.ManagerId = null;
        await db.Tasks.Where(t => t.AssignedToId == userId)
            .ForEachAsync(t => t.AssignedToId = null);
        db.Comments.RemoveRange(db.Comments.Where(c => c.AuthorId == userId));
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}
