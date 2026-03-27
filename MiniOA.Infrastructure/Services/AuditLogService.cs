using Microsoft.EntityFrameworkCore;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;

namespace MiniOA.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogStatusChangeAsync(int taskId, int userId, myTaskStatus fromStatus, myTaskStatus toStatus, string? remarks = null)
        {
            var auditLog = new TaskAuditLog
            {
                TaskId = taskId,
                UserId = userId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                OperationType = "StatusChange",
                Remarks = remarks
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogTaskCreationAsync(int taskId, int userId)
        {
            var auditLog = new TaskAuditLog
            {
                TaskId = taskId,
                UserId = userId,
                FromStatus = myTaskStatus.Todo,  // 创建时默认为Todo状态
                ToStatus = myTaskStatus.Todo,
                OperationType = "Create",
                Remarks = "任务创建"
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogTaskUpdateAsync(int taskId, int userId, Dictionary<string, (object? oldValue, object? newValue)> changes)
        {
            var changeDetails = new List<string>();
            foreach (var change in changes)
            {
                if (change.Value.oldValue?.ToString() != change.Value.newValue?.ToString())
                {
                    changeDetails.Add($"{change.Key}: [{change.Value.oldValue}] -> [{change.Value.newValue}]");
                }
            }

            if (changeDetails.Any())
            {
                var auditLog = new TaskAuditLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    FromStatus = myTaskStatus.Todo,
                    ToStatus = myTaskStatus.Todo,
                    OperationType = "Update",
                    Remarks = "任务更新: " + string.Join("; ", changeDetails)
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogTaskDeletionAsync(int taskId, int userId, string taskTitle)
        {
            var auditLog = new TaskAuditLog
            {
                TaskId = taskId,
                UserId = userId,
                FromStatus = myTaskStatus.Todo,
                ToStatus = myTaskStatus.Todo,
                OperationType = "Delete",
                Remarks = $"任务删除: {taskTitle}"
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TaskAuditLog>> GetTaskAuditLogsAsync(int taskId)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.TaskId == taskId)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskAuditLog>> GetUserAuditLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.Task)
                .Where(al => al.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(al => al.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();
        }
    }
}
