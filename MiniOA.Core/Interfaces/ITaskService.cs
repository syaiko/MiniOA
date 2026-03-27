using MiniOA.Core.DTOs;
using MiniOA.Core.Models;

namespace MiniOA.Core.Interfaces
{
    public interface ITaskService
    {
        Task<ApiResult<List<TaskDto>>> GetTasksAsync(int currentUserId, bool isAdmin);
        Task<ApiResult<TaskDto>> GetTaskByIdAsync(int id, int currentUserId, bool isAdmin);
        Task<ApiResult<TaskDto>> CreateTaskAsync(CreateTaskDto input, int currentUserId);
        Task<ApiResult<TaskDto>> UpdateTaskAsync(int id, UpdateTaskDto input, int currentUserId, bool isAdmin);
        Task<ApiResult<bool>> DeleteTaskAsync(int id, int currentUserId, bool isAdmin);
        Task<ApiResult<bool>> StartProcessingAsync(int id, int currentUserId, bool isAdmin);
        Task<ApiResult<bool>> SubmitReviewAsync(int id, int currentUserId, bool isAdmin);
        Task<ApiResult<bool>> ApproveTaskAsync(int id, bool isPass, int currentUserId, bool isAdmin); 
        Task<ApiResult<List<AuditLogDto>>> GetTaskAuditLogsAsync(int taskId, int currentUserId, bool isAdmin);
        Task<ApiResult<TaskStatisticsDto>> GetTaskStatisticsAsync(int currentUserId, bool isAdmin);
        Task<ApiResult<TaskTrendDto>> GetTaskTrendAsync(int currentUserId, bool isAdmin, int months = 12);
        Task<ApiResult<DashboardDto>> GetDashboardAsync(int currentUserId);
    }
}
