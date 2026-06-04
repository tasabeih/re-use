using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ReUse.API.Extensions;
using ReUse.Application.Interfaces.Services;

namespace ReUse.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserActivities(Guid userId, int limit = 50)
    {
        var items = await _activityService.GetUserActivitiesAsync(userId, limit);
        return Ok(items);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyActivities(int limit = 50)
    {
        var userId = User.GetBusinessId();
        var items = await _activityService.GetUserActivitiesAsync(userId, limit);
        return Ok(items);
    }
}