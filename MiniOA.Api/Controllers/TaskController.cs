using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using Newtonsoft.Json;

namespace MiniOA.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TaskController : BaseController
    {
        private readonly ITaskService _taskService;
        private readonly IMapper _mapper;

        public TaskController(ITaskService taskService,IMapper mapper)
        {
            _taskService = taskService;
            _mapper = mapper;
        }

        //获取任务列表 - 员工看自己，管理员看所有
        [HttpGet("getMyTasks")]
        public async Task<ApiResult<List<TaskDto>>> GetTasks()
        {
            return await _taskService.GetTasksAsync(CurrentUserId, IsAdmin);
        }

        //获取任务详情
        [HttpGet("{id}")]
        public async Task<ApiResult<TaskDto>> GetTaskById(int id)
        {
            return await _taskService.GetTaskByIdAsync(id, CurrentUserId, IsAdmin);
        }

        //创建新任务
        [HttpPost("createTask")]
        public async Task<ApiResult<TaskDto>> CreateTask([FromBody] CreateTaskDto input)
        {
            return await _taskService.CreateTaskAsync(input, CurrentUserId);
        }

        //更新任务
        [HttpPut("{id}")]
        public async Task<ApiResult<TaskDto>> UpdateTask(int id, [FromBody] UpdateTaskDto input)
        {
            return await _taskService.UpdateTaskAsync(id, input, CurrentUserId, IsAdmin);
        }

        //删除任务
        [HttpDelete("{id}")]
        public async Task<ApiResult<bool>> DeleteTask(int id)
        {
            return await _taskService.DeleteTaskAsync(id, CurrentUserId, IsAdmin);
        }

        //开始处理任务  <--员工
        [HttpPost("{id}/start-processing")]
        public async Task<ApiResult<bool>> StartProcessing(int id)
        {
            return await _taskService.StartProcessingAsync(id, CurrentUserId, IsAdmin);
        }

        //更新任务进度  <--员工
        [HttpPost("{id}/submit-review")]
        public async Task<ApiResult<bool>> SubmitReview(int id)
        {
            return await _taskService.SubmitReviewAsync(id, CurrentUserId, IsAdmin);
        }

        //审批任务  <--部门经理级以上
        [HttpPost("{id}/approve")]
        public async Task<ApiResult<bool>> ApproveTask(int id, [FromQuery] bool isPass)
        {
            return await _taskService.ApproveTaskAsync(id, isPass, CurrentUserId, IsDepartmentManagerOrAbove);
        }

        //获取任务审计日志  <--组长级以上及创建者
        [HttpGet("{id}/audit-logs")]
        public async Task<ApiResult<List<AuditLogDto>>> GetTaskAuditLogs(int id)
        {
            return await _taskService.GetTaskAuditLogsAsync(id, CurrentUserId, IsTeamLeaderOrAbove);
        }

        //获取任务统计报表  <--管理员
        [HttpGet("statistics")]
        public async Task<ApiResult<TaskStatisticsDto>> GetTaskStatistics()
        {
            return await _taskService.GetTaskStatisticsAsync(CurrentUserId, IsAdmin);
        }

        //获取任务趋势数据  <--管理员
        [HttpGet("trend")]
        public async Task<ApiResult<TaskTrendDto>> GetTaskTrend([FromQuery] int months = 12)
        {
            return await _taskService.GetTaskTrendAsync(CurrentUserId, IsAdmin, months);
        }

        //获取首页仪表盘数据
        [HttpGet("dashboard")]
        public async Task<ApiResult<DashboardDto>> GetDashboard()
        {
            return await _taskService.GetDashboardAsync(CurrentUserId);
        }
    }
}
