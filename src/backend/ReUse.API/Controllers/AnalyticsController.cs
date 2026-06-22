using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ReUse.Application.DTOs.Analytics;
using ReUse.Application.Enums;
using ReUse.Application.Interfaces.Services;

namespace ReUse.API.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
[Tags("Dashboard - Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DashboardPeriod period = DashboardPeriod.Last7Days,
        [FromQuery] int productPage = 0,
        [FromQuery] int productPageSize = 10,
        [FromQuery] int sellerPage = 0,
        [FromQuery] int sellerPageSize = 10)
    {
        if (!Enum.IsDefined(typeof(DashboardPeriod), period))
            return BadRequest("Invalid period value.");

        var result = await _analyticsService.GetDashboardAsync(period, productPage, productPageSize, sellerPage, sellerPageSize);

        return Ok(result);
    }
}