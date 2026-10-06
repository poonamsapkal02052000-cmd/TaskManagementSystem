using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Common;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Task status overview (overall and per user) for the tasks the caller can see.
    /// Supports filtering by status, priority, deadline range and team.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] TaskQuery filter) =>
        Ok(await dashboard.GetAsync(User.ToCurrentUser(), filter));
}
