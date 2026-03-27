using MiniOA.Core.Models;

namespace MiniOA.Core.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// 获取未读通知数量
        /// </summary>
        Task<ApiResult<int>> GetUnreadCountAsync(int userId);

        /// <summary>
        /// 获取通知列表（分页）
        /// </summary>
        Task<ApiResult<object>> GetNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20);

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        Task<ApiResult<bool>> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// 标记所有通知为已读
        /// </summary>
        Task<ApiResult<int>> MarkAllAsReadAsync(int userId);
    }
}
