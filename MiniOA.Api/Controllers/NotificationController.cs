using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;

namespace MiniOA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// 获取未读通知数量
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var result = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 获取通知列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _notificationService.GetNotificationsAsync(CurrentUserId, isRead, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            if (!result.Success && result.Code == 404)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// 标记所有通知为已读
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return Ok(result);
        }
    }
}
