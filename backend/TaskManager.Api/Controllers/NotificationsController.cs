using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController(INotificationService notifications) : ControllerBase
{
    /// <summary>Latest 50 notifications for the current user.</summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll([FromQuery] bool unreadOnly = false) =>
        Ok(await notifications.GetForUserAsync(User.ToCurrentUser().Id, unreadOnly));

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount() =>
        Ok(new { count = await notifications.UnreadCountAsync(User.ToCurrentUser().Id) });

    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(int id)
    {
        await notifications.MarkReadAsync(User.ToCurrentUser().Id, id);
        return NoContent();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead()
    {
        await notifications.MarkAllReadAsync(User.ToCurrentUser().Id);
        return NoContent();
    }
}
