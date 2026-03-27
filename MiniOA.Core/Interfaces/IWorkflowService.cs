using MiniOA.Core.DTOs;
using MiniOA.Core.Models;

namespace MiniOA.Core.Interfaces
{
    public interface IWorkflowService
    {
        Task<ApiResult<WorkflowInstanceDto>> SubmitWorkflowAsync(int currentUserId, SubmitWorkflowInputDto input);
        Task<ApiResult<WorkflowInstanceDto>> ResubmitWorkflowAsync(int currentUserId, int instanceId, SubmitWorkflowInputDto input);
        Task<ApiResult<WorkflowInstanceDto>> ApproveAsync(int currentUserId, ApproveInputDto input);
        Task<ApiResult<WorkflowInstanceDto>> GetInstanceAsync(int id);
        Task<ApiResult<List<WorkflowInstanceDto>>> GetMyPendingAsync(int userId);
        Task<ApiResult<List<WorkflowInstanceDto>>> GetMyDoneAsync(int userId);
        Task<ApiResult<List<WorkflowInstanceDto>>> GetMyInstancesAsync(int userId);
        Task<ApiResult<List<WorkflowNodeDto>>> GetWorkflowNodesAsync(string workflowType);
    }
}
