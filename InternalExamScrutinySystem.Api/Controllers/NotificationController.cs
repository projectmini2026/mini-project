using System.Security.Claims;
using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value!);
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<InternalExamScrutinySystem.Api.Data.Notification>>>> GetMyNotifications(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var items = await _notificationService.GetMyNotificationsAsync(userId, cancellationToken);
        return Ok(new ApiResponse<List<InternalExamScrutinySystem.Api.Data.Notification>>
        {
            success = true,
            message = "Notifications fetched.",
            data = items
        });
    }

    [HttpPost("me/mark-read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _notificationService.MarkAsReadAsync(userId, cancellationToken);
        return Ok(new ApiResponse<object>
        {
            success = true,
            message = "Marked as read.",
            data = null
        });
    }
}

