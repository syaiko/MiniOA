using Microsoft.EntityFrameworkCore;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Infrastructure;

namespace MiniOA.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取未读通知数量
        /// </summary>
        public async Task<ApiResult<int>> GetUnreadCountAsync(int userId)
        {
            var count = await _context.Notifications
                .CountAsync(n => n.ReceiverId == userId && !n.IsRead);

            return ApiResult<int>.Ok(count, "获取未读数量成功");
        }

        /// <summary>
        /// 获取通知列表（分页）
        /// </summary>
        public async Task<ApiResult<object>> GetNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20)
        {
            var query = _context.Notifications
                .Include(n => n.Receiver)
                .Where(n => n.ReceiverId == userId);

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    n.Type,
                    n.IsRead,
                    n.ReadAt,
                    n.RelatedId,
                    n.RelatedType,
                    n.CreatedAt,
                    ReceiverName = n.Receiver.FullName ?? n.Receiver.Username
                })
                .ToListAsync();

            return ApiResult<object>.Ok(new { notifications, totalCount, page, pageSize }, "获取通知列表成功");
        }

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        public async Task<ApiResult<bool>> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == userId);

            if (notification == null)
                return ApiResult<bool>.Fail("通知不存在", 404);

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "已标记为已读");
        }

        /// <summary>
        /// 标记所有通知为已读
        /// </summary>
        public async Task<ApiResult<int>> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ApiResult<int>.Ok(unreadNotifications.Count, "已全部标记为已读");
        }
    }
}
