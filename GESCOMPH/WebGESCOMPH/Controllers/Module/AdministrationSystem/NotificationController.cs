using Business.Interfaces.Implements.AdministrationSystem;
using Entity.DTOs.Implements.Utilities;
using Entity.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebGESCOMPH.Controllers.Base;

namespace WebGESCOMPH.Controllers.Module.AdministrationSystem
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class NotificationController
        : BaseController<NotificationDto, NotificationCreateDto, NotificationUpdateDto>
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
            : base(notificationService, logger)
        {
            _notificationService = notificationService;
        }

        [HttpGet("feed/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetFeed(
            int userId,
            [FromQuery] NotificationStatus? status = null,
            [FromQuery] int take = 20)
        {
            var items = await _notificationService.GetFeedAsync(userId, status, take);
            return Ok(items);
        }

        [HttpGet("{userId:int}/unread")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnread(int userId)
        {
            var items = await _notificationService.GetUnreadAsync(userId);
            return Ok(items);
        }

        [HttpPatch("{notificationId:int}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(
            int notificationId,
            [FromQuery] int userId)
        {
            return await _notificationService.MarkAsReadAsync(notificationId, userId)
                ? NoContent()
                : NotFound();
        }

        [HttpPatch("mark-all/{userId:int}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }
    }
}
