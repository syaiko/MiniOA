using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MiniOA.Infrastructure.Hubs
{
    // SignalR通知中心
    [Authorize]
    public class NotificationHub : Hub
    {
        // 存储用户连接映射（userId -> connectionId）
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();

        // 用户连接时记录userId
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                // 记录用户连接
                _userConnections[userId.Value] = Context.ConnectionId;
                
                // 加入用户组
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
            }

            await base.OnConnectedAsync();
        }

        // 用户断开连接时移除记录
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                _userConnections.TryRemove(userId.Value, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 发送消息给指定用户
        public async Task SendToUser(int userId, string message)
        {
            if (_userConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
            }
        }

        // 发送消息给指定用户组
        public async Task SendToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
        }

        // 获取当前用户ID
        private int? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst("sub") ?? Context.User?.FindFirst("id");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        // 静态方法：获取用户连接ID
        public static string? GetConnectionId(int userId)
        {
            _userConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}
