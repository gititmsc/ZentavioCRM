using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Notifications;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Produces("application/json")]
    [Authorize]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetRecentAsync(userId.Value);
            return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.SuccessResponse(notifications));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(ApiResponse<int>.SuccessResponse(count));
        }

        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
            return success
                ? Ok(ApiResponse<bool>.SuccessResponse(true))
                : NotFound(ApiResponse<bool>.FailureResponse("Notification not found."));
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
    }
}
