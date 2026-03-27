using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Interfaces
{
    public interface IAuditLogService
    {
        Task LogStatusChangeAsync(int taskId, int userId, myTaskStatus fromStatus, myTaskStatus toStatus, string? remarks = null);
        Task LogTaskCreationAsync(int taskId, int userId);
        Task LogTaskUpdateAsync(int taskId, int userId, Dictionary<string, (object? oldValue, object? newValue)> changes);
        Task LogTaskDeletionAsync(int taskId, int userId, string taskTitle);
        Task<List<TaskAuditLog>> GetTaskAuditLogsAsync(int taskId);
        Task<List<TaskAuditLog>> GetUserAuditLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
