using System.Security.Claims;
using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value!);
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyDashboard(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var response = await _dashboardService.GetMyDashboardAsync(userId, cancellationToken);
        return Ok(response);
    }
}

