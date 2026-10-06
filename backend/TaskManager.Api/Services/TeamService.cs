using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface ITeamService
{
    Task<List<TeamDto>> GetTeamsAsync(CurrentUser actor);
    Task<TeamDto> GetTeamAsync(CurrentUser actor, int id);
    Task<TeamDto> CreateAsync(CreateTeamRequest request);
    Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request);
    Task DeleteAsync(int id);
    Task<TeamDto> AddMemberAsync(CurrentUser actor, int teamId, int userId);
    Task<TeamDto> RemoveMemberAsync(CurrentUser actor, int teamId, int userId);
}

public class TeamService(AppDbContext db) : ITeamService
{
    private IQueryable<Team> Teams => db.Teams.Include(t => t.Manager).Include(t => t.Members);

    public async Task<List<TeamDto>> GetTeamsAsync(CurrentUser actor)
    {
        var query = actor.Role switch
        {
            UserRole.Admin => Teams,
            UserRole.Manager => Teams.Where(t => t.ManagerId == actor.Id),
            _ => Teams.Where(t => t.Members.Any(m => m.Id == actor.Id))
        };
        var teams = await query.OrderBy(t => t.Name).ToListAsync();
        return teams.Select(t => t.ToDto()).ToList();
    }

    public async Task<TeamDto> GetTeamAsync(CurrentUser actor, int id)
    {
        var team = await Teams.FirstOrDefaultAsync(t => t.Id == id)
                   ?? throw new NotFoundException("Team not found.");

        var allowed = actor.Role == UserRole.Admin
                      || team.ManagerId == actor.Id
                      || team.Members.Any(m => m.Id == actor.Id);
        if (!allowed) throw new ForbiddenException();

        return team.ToDto();
    }

    public async Task<TeamDto> CreateAsync(CreateTeamRequest request)
    {
        await EnsureUniqueName(request.Name);
        await EnsureValidManager(request.ManagerId);

        var team = new Team { Name = request.Name.Trim(), Description = request.Description?.Trim(), ManagerId = request.ManagerId };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return await LoadDto(team.Id);
    }

    public async Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request)
    {
        var team = await db.Teams.FindAsync(id) ?? throw new NotFoundException("Team not found.");
        await EnsureUniqueName(request.Name, id);
        await EnsureValidManager(request.ManagerId);

        team.Name = request.Name.Trim();
        team.Description = request.Description?.Trim();
        team.ManagerId = request.ManagerId;
        await db.SaveChangesAsync();
        return await LoadDto(id);
    }

    public async Task DeleteAsync(int id)
    {
        var team = await db.Teams.Include(t => t.Members).Include(t => t.Tasks).FirstOrDefaultAsync(t => t.Id == id)
                   ?? throw new NotFoundException("Team not found.");

        foreach (var m in team.Members) m.TeamId = null;
        foreach (var t in team.Tasks) t.TeamId = null;
        db.Teams.Remove(team);
        await db.SaveChangesAsync();
    }

    /// <summary>Admins can add anyone; Managers can add unassigned Users to the teams they manage.</summary>
    public async Task<TeamDto> AddMemberAsync(CurrentUser actor, int teamId, int userId)
    {
        var team = await db.Teams.FindAsync(teamId) ?? throw new NotFoundException("Team not found.");
        EnsureCanManage(actor, team);

        var user = await db.Users.FindAsync(userId) ?? throw new NotFoundException("User not found.");
        if (user.Role == UserRole.Admin) throw new BadRequestException("Admins cannot be team members.");
        if (user.TeamId == teamId) throw new ConflictException("User is already a member of this team.");

        if (actor.Role == UserRole.Manager)
        {
            if (user.Role != UserRole.User)
                throw new ForbiddenException("Managers can only add users with the 'User' role.");
            if (user.TeamId != null)
                throw new ConflictException("User already belongs to another team. Ask an Admin to move them.");
        }

        user.TeamId = teamId;
        await db.SaveChangesAsync();
        return await LoadDto(teamId);
    }

    public async Task<TeamDto> RemoveMemberAsync(CurrentUser actor, int teamId, int userId)
    {
        var team = await db.Teams.FindAsync(teamId) ?? throw new NotFoundException("Team not found.");
        EnsureCanManage(actor, team);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TeamId == teamId)
                   ?? throw new NotFoundException("User is not a member of this team.");

        user.TeamId = null;
        await db.SaveChangesAsync();
        return await LoadDto(teamId);
    }

    private static void EnsureCanManage(CurrentUser actor, Team team)
    {
        if (actor.Role == UserRole.Admin) return;
        if (actor.Role == UserRole.Manager && team.ManagerId == actor.Id) return;
        throw new ForbiddenException("Only an Admin or this team's Manager can change its members.");
    }

    private async Task EnsureUniqueName(string name, int? excludeId = null)
    {
        var trimmed = name.Trim();
        if (await db.Teams.AnyAsync(t => t.Name == trimmed && t.Id != excludeId))
            throw new ConflictException($"A team named '{trimmed}' already exists.");
    }

    private async Task EnsureValidManager(int? managerId)
    {
        if (managerId is null) return;
        var manager = await db.Users.FindAsync(managerId.Value)
                      ?? throw new BadRequestException("Manager not found.");
        if (manager.Role != UserRole.Manager)
            throw new BadRequestException("The selected user does not have the Manager role.");
    }

    private async Task<TeamDto> LoadDto(int id) => (await Teams.FirstAsync(t => t.Id == id)).ToDto();
}
