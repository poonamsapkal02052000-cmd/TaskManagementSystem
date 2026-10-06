using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
[Produces("application/json")]
public class TeamsController(ITeamService teams) : ControllerBase
{
    /// <summary>Admins see all teams, Managers see teams they manage, Users see their own team.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetAll() =>
        Ok(await teams.GetTeamsAsync(User.ToCurrentUser()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamDto>> Get(int id) =>
        Ok(await teams.GetTeamAsync(User.ToCurrentUser(), id));

    /// <summary>Create a team (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TeamDto>> Create(CreateTeamRequest request)
    {
        var team = await teams.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = team.Id }, team);
    }

    /// <summary>Update a team's name, description or manager (Admin only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeamDto>> Update(int id, UpdateTeamRequest request) =>
        Ok(await teams.UpdateAsync(id, request));

    /// <summary>Delete a team (Admin only). Members and tasks are kept but unlinked.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await teams.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Add a member to a team (Admin, or the team's Manager).</summary>
    [HttpPost("{id:int}/members")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TeamDto>> AddMember(int id, AddTeamMemberRequest request) =>
        Ok(await teams.AddMemberAsync(User.ToCurrentUser(), id, request.UserId));

    /// <summary>Remove a member from a team (Admin, or the team's Manager).</summary>
    [HttpDelete("{id:int}/members/{userId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TeamDto>> RemoveMember(int id, int userId) =>
        Ok(await teams.RemoveMemberAsync(User.ToCurrentUser(), id, userId));
}
