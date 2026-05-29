using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using ReUse.Application.DTOs;
using ReUse.Application.Interfaces.Services;

namespace ReUse.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivities(Guid userId, int limit = 50)
    {
        var items = await _activityService.GetUserActivitiesAsync(userId, limit);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ActivityEventDto dto)
    {
        await _activityService.CreateActivityAsync(dto.UserId, dto.ProductId, dto.Type, dto.Description, dto.Metadata);
        return CreatedAtAction(nameof(GetUserActivities), new { userId = dto.UserId }, null);
    }
}