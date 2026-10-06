using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin,Manager")]
[Produces("application/json")]
public class UsersController(IUserService users) : ControllerBase
{
    /// <summary>List users. Managers do not see Admin accounts.</summary>
    /// <param name="role">Optional role filter.</param>
    /// <param name="unassigned">Only users without a team.</param>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] UserRole? role, [FromQuery] bool unassigned = false) =>
        Ok(await users.GetUsersAsync(User.ToCurrentUser(), role, unassigned));

    /// <summary>Create a user with any role (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = await users.CreateUserAsync(request);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Change a user's role (Admin only).</summary>
    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> UpdateRole(int id, UpdateUserRoleRequest request) =>
        Ok(await users.UpdateRoleAsync(User.ToCurrentUser(), id, request.Role));

    /// <summary>Delete a user (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await users.DeleteUserAsync(User.ToCurrentUser(), id);
        return NoContent();
    }
}
