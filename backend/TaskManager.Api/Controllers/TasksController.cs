using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController(ITaskService tasks, ICommentService comments) : ControllerBase
{
    /// <summary>List tasks visible to the current user with filtering, sorting and paging.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetAll([FromQuery] TaskQuery query) =>
        Ok(await tasks.GetTasksAsync(User.ToCurrentUser(), query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> Get(int id) =>
        Ok(await tasks.GetTaskAsync(User.ToCurrentUser(), id));

    /// <summary>Create a task (Admin / Manager). The assignee is notified.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request)
    {
        var task = await tasks.CreateAsync(User.ToCurrentUser(), request);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>Edit task details (Admin / owning Manager).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskRequest request) =>
        Ok(await tasks.UpdateAsync(User.ToCurrentUser(), id, request));

    /// <summary>Assign or unassign a task (Admin / owning Manager). The new assignee is notified.</summary>
    [HttpPatch("{id:int}/assign")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TaskDto>> Assign(int id, AssignTaskRequest request) =>
        Ok(await tasks.AssignAsync(User.ToCurrentUser(), id, request.AssignedToId));

    /// <summary>Change task status (To Do / In Progress / Done). Creator and assignee are notified.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(int id, UpdateTaskStatusRequest request) =>
        Ok(await tasks.UpdateStatusAsync(User.ToCurrentUser(), id, request.Status));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await tasks.DeleteAsync(User.ToCurrentUser(), id);
        return NoContent();
    }

    // ---------- Comments ----------

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(int id) =>
        Ok(await comments.GetAsync(User.ToCurrentUser(), id));

    [HttpPost("{id:int}/comments")]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentRequest request)
    {
        var comment = await comments.AddAsync(User.ToCurrentUser(), id, request.Content);
        return StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpDelete("{id:int}/comments/{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteComment(int id, int commentId)
    {
        await comments.DeleteAsync(User.ToCurrentUser(), id, commentId);
        return NoContent();
    }
}
