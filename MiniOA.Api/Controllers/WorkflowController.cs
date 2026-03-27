using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniOA.Core.DTOs;
using MiniOA.Core.Interfaces;

namespace MiniOA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkflowController : BaseController
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        /// <summary>
        /// 提交流程
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitWorkflowInputDto input)
        {
            var result = await _workflowService.SubmitWorkflowAsync(CurrentUserId, input);
            return Ok(result);
        }

        /// <summary>
        /// 重新提交流程（已退回的流程）
        /// </summary>
        [HttpPost("{id}/resubmit")]
        public async Task<IActionResult> Resubmit(int id, [FromBody] SubmitWorkflowInputDto input)
        {
            var result = await _workflowService.ResubmitWorkflowAsync(CurrentUserId, id, input);
            return Ok(result);
        }

        /// <summary>
        /// 审批流程
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] ApproveInputDto input)
        {
            var result = await _workflowService.ApproveAsync(CurrentUserId, input);
            return Ok(result);
        }

        /// <summary>
        /// 获取流程实例详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInstance(int id)
        {
            var result = await _workflowService.GetInstanceAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// 获取我的申请列表
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyInstances()
        {
            var result = await _workflowService.GetMyInstancesAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 获取我的待办流程
        /// </summary>
        [HttpGet("todo")]
        public async Task<IActionResult> GetMyTodo()
        {
            var result = await _workflowService.GetMyPendingAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 获取我的已办流程
        /// </summary>
        [HttpGet("done")]
        public async Task<IActionResult> GetMyDone()
        {
            var result = await _workflowService.GetMyDoneAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 获取流程节点列表
        /// </summary>
        [HttpGet("nodes/{workflowType}")]
        public async Task<IActionResult> GetWorkflowNodes(string workflowType)
        {
            var result = await _workflowService.GetWorkflowNodesAsync(workflowType);
            return Ok(result);
        }
    }
}
