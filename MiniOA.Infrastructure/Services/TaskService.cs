using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Infrastructure;
using MiniOA.Infrastructure.Hubs;
using Newtonsoft.Json;

namespace MiniOA.Infrastructure.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TaskService(AppDbContext context, IMapper mapper, IAuditLogService auditLogService, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _hubContext = hubContext;
        }

        public async Task<ApiResult<List<TaskDto>>> GetTasksAsync(int currentUserId, bool isAdmin)
        {
            var query = _context.Tasks.Include(t => t.User).AsQueryable();

            if (!isAdmin)
            {
                // 非管理员可以看到：自己创建的任务 或 指派给自己的任务
                query = query.Where(t => t.UserId == currentUserId || t.AssignedUserId == currentUserId);
            }

            var tasks = await query.ToListAsync();

            // 检查逾期任务并更新状态
            await CheckAndUpdateOverdueTasks(tasks);

            var taskDtos = _mapper.Map<List<TaskDto>>(tasks);
            return ApiResult<List<TaskDto>>.Ok(taskDtos, "获取任务列表成功");
        }

        public async Task<ApiResult<TaskDto>> GetTaskByIdAsync(int id, int currentUserId, bool isAdmin)
        {
            var task = await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.Department)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return ApiResult<TaskDto>.Fail("任务不存在", 404);

            // 权限检查：管理员可以查看所有任务，普通用户只能查看自己创建或指派给自己的任务
            if (!isAdmin && task.UserId != currentUserId && task.AssignedUserId != currentUserId)
                return ApiResult<TaskDto>.Fail("无权查看此任务", 403);

            var taskDto = _mapper.Map<TaskDto>(task);
            return ApiResult<TaskDto>.Ok(taskDto, "获取任务详情成功");
        }

        private async Task CheckAndUpdateOverdueTasks(List<TodoTask> tasks)
        {
            var now = DateTime.UtcNow;
            var overdueTasks = tasks.Where(t => 
                t.DueDate.HasValue && 
                t.DueDate.Value < now && 
                t.Status != myTaskStatus.Completed && 
                t.Status != myTaskStatus.Overdue
            ).ToList();

            if (overdueTasks.Any())
            {
                foreach (var task in overdueTasks)
                {
                    task.Status = myTaskStatus.Overdue;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ApiResult<TaskDto>> CreateTaskAsync(CreateTaskDto input, int currentUserId)
        {
            var newTask = _mapper.Map<TodoTask>(input);
            newTask.UserId = currentUserId;

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            // 记录创建审计日志
            await _auditLogService.LogTaskCreationAsync(newTask.Id, currentUserId);

            // 发送实时通知给被指派用户
            if (input.AssignedUserId.HasValue)
            {
                var creator = await _context.Users.FindAsync(currentUserId);
                var notification = new Notification
                {
                    Title = "新任务指派",
                    Content = $"【{creator?.FullName ?? creator?.Username}】给您指派了新任务：【{newTask.Title}】",
                    ReceiverId = input.AssignedUserId.Value,
                    Type = NotificationType.Task,
                    RelatedId = newTask.Id,
                    RelatedType = "Task",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 通过SignalR推送实时通知
                var connectionId = NotificationHub.GetConnectionId(input.AssignedUserId.Value);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    var notificationMessage = new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        content = notification.Content,
                        type = notification.Type.ToString(),
                        relatedId = notification.RelatedId,
                        relatedType = notification.RelatedType,
                        createdAt = notification.CreatedAt
                    };
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationMessage);
                }
            }

            var resultDto = _mapper.Map<TaskDto>(newTask);
            return ApiResult<TaskDto>.Ok(resultDto, "创建任务成功");
        }

        public async Task<ApiResult<TaskDto>> UpdateTaskAsync(int id, UpdateTaskDto input, int currentUserId, bool isAdmin)
        {
            var task = await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (task == null)
                return ApiResult<TaskDto>.Fail("任务不存在", 404);

            // 权限检查：管理员或任务创建者可以编辑
            if (!isAdmin && task.UserId != currentUserId)
                return ApiResult<TaskDto>.Fail("无权编辑此任务", 403);

            // 只有待处理状态的任务可以编辑
            if (task.Status != myTaskStatus.Todo)
                return ApiResult<TaskDto>.Fail("只有待处理状态的任务才能编辑", 400);

            // 记录修改前的值
            var oldTitle = task.Title;
            var oldDescription = task.Description;
            var oldPriority = task.Priority;
            var oldDueDate = task.DueDate;
            var oldDepartmentId = task.DepartmentId;
            var oldAssignedUserId = task.AssignedUserId;

            // 更新任务属性
            task.Title = input.Title;
            task.Description = input.Description;
            task.Priority = input.Priority;
            task.DueDate = input.DueDate;
            task.DepartmentId = input.DepartmentId;
            task.AssignedUserId = input.AssignedUserId;

            await _context.SaveChangesAsync();

            // 记录审计日志
            await _auditLogService.LogTaskUpdateAsync(task.Id, currentUserId, new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "Title", (oldTitle, task.Title) },
                { "Description", (oldDescription, task.Description) },
                { "Priority", (oldPriority, task.Priority) },
                { "DueDate", (oldDueDate?.ToString("yyyy-MM-dd HH:mm:ss"), task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss")) },
                { "DepartmentId", (oldDepartmentId, task.DepartmentId) },
                { "AssignedUserId", (oldAssignedUserId, task.AssignedUserId) }
            });

            // 如果指派用户变更，发送通知
            if (oldAssignedUserId != task.AssignedUserId && task.AssignedUserId.HasValue)
            {
                var editor = await _context.Users.FindAsync(currentUserId);
                var notification = new Notification
                {
                    Title = "任务指派变更",
                    Content = $"【{editor?.FullName ?? editor?.Username}】将任务【{task.Title}】指派给了您",
                    ReceiverId = task.AssignedUserId.Value,
                    Type = NotificationType.Task,
                    RelatedId = task.Id,
                    RelatedType = "Task",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var connectionId = NotificationHub.GetConnectionId(task.AssignedUserId.Value);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        content = notification.Content,
                        type = notification.Type.ToString(),
                        relatedId = notification.RelatedId,
                        relatedType = notification.RelatedType,
                        createdAt = notification.CreatedAt
                    });
                }
            }

            var updatedDto = _mapper.Map<TaskDto>(task);
            return ApiResult<TaskDto>.Ok(updatedDto, "更新任务成功");
        }

        public async Task<ApiResult<bool>> DeleteTaskAsync(int id, int currentUserId, bool isAdmin)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return ApiResult<bool>.Fail("任务不存在", 404);

            // 权限检查：管理员或任务创建者可以删除
            if (!isAdmin && task.UserId != currentUserId)
                return ApiResult<bool>.Fail("无权删除此任务", 403);

            // 只有待处理状态的任务可以删除
            if (task.Status != myTaskStatus.Todo)
                return ApiResult<bool>.Fail("只有待处理状态的任务才能删除", 400);

            // 记录审计日志
            await _auditLogService.LogTaskDeletionAsync(task.Id, currentUserId, task.Title);

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "删除任务成功");
        }

        public async Task<ApiResult<bool>> StartProcessingAsync(int id, int currentUserId, bool isAdmin)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return ApiResult<bool>.Fail("任务不存在", 404);

            if (task.UserId != currentUserId && !isAdmin)
                return ApiResult<bool>.Fail("无权操作他人任务", 403);

            if (task.Status != myTaskStatus.Todo)
                return ApiResult<bool>.Fail("只有待处理状态的任务才能开始处理", 400);

            var fromStatus = task.Status;
            task.Status = myTaskStatus.Processing;
            await _context.SaveChangesAsync();

            // 记录状态变更审计日志
            await _auditLogService.LogStatusChangeAsync(id, currentUserId, fromStatus, task.Status, "开始处理任务");

            return ApiResult<bool>.Ok(true, "已开始处理任务");
        }

        public async Task<ApiResult<bool>> SubmitReviewAsync(int id, int currentUserId, bool isAdmin)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return ApiResult<bool>.Fail("任务不存在", 404);

            if (task.UserId != currentUserId && !isAdmin)
                return ApiResult<bool>.Fail("无权操作他人任务", 403);

            var fromStatus = task.Status;
            task.Status = myTaskStatus.Review;
            await _context.SaveChangesAsync();

            // 记录状态变更审计日志
            await _auditLogService.LogStatusChangeAsync(id, currentUserId, fromStatus, task.Status, "提交审核");

            // 发送通知给所有部门经理及以上角色
            await CreateTaskReviewNotificationsAsync(task, currentUserId);

            return ApiResult<bool>.Ok(true, "已提交审核");
        }

        public async Task<ApiResult<bool>> ApproveTaskAsync(int id, bool isPass, int currentUserId, bool isDepartmentManagerOrAbove)
        {
            if (!isDepartmentManagerOrAbove) return ApiResult<bool>.Fail("只有主管有权审批", 403);

            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return ApiResult<bool>.Fail("任务不存在", 404);

            var fromStatus = task.Status;
            if (isPass)
                task.Status = myTaskStatus.Completed;
            else
                task.Status = myTaskStatus.Rejected;

            await _context.SaveChangesAsync();

            // 记录状态变更审计日志
            await _auditLogService.LogStatusChangeAsync(id, currentUserId, fromStatus, task.Status, isPass ? "审批通过" : "审批驳回");

            // 发送通知给任务创建者
            await CreateTaskApprovalNotificationAsync(task, currentUserId, isPass);

            return ApiResult<bool>.Ok(true, isPass ? "审批通过" : "已驳回");
        }


        public async Task<ApiResult<List<AuditLogDto>>> GetTaskAuditLogsAsync(int taskId, int currentUserId, bool isTeamLeaderOrAbove)
        {
            // 1. 获取任务信息
            var taskResult = await _context.Tasks.FindAsync(taskId);
            if (taskResult == null)
            {
                return ApiResult<List<AuditLogDto>>.Fail("任务不存在", 404);
            }

            // 2. 权限验证：组长级以上或任务创建者
            if (!isTeamLeaderOrAbove && taskResult.UserId != currentUserId)
            {
                return ApiResult<List<AuditLogDto>>.Fail("无权限查看该任务的审计日志", 403);
            }

            // 3. 获取审计日志
            var auditLogs = await _auditLogService.GetTaskAuditLogsAsync(taskId);

            // 4. 映射为 DTO
            var auditLogDtos = auditLogs.Select(log => new AuditLogDto
            {
                Id = log.Id,
                TaskId = log.TaskId,
                UserId = log.UserId,
                Username = log.User?.Username ?? "",
                FullName = log.User?.FullName,
                FromStatus = log.FromStatus,
                ToStatus = log.ToStatus,
                FromStatusDisplay = log.FromStatus.ToString(),
                ToStatusDisplay = log.ToStatus.ToString(),
                CreatedAt = log.CreatedAt,
                OperationType = log.OperationType,
                Remarks = log.Remarks
            }).ToList();

            return ApiResult<List<AuditLogDto>>.Ok(auditLogDtos, "获取审计日志成功");
        }

        // 创建任务审核通知
        private async Task CreateTaskReviewNotificationsAsync(TodoTask task, int submitterId)
        {
            try
            {
                // 获取提交者信息
                var submitter = await _context.Users.FindAsync(submitterId);
                
                // 查找所有部门经理及以上角色的用户
                var approvers = await _context.Users
                    .Where(u => (u.Role == UserRole.DepartmentManager || 
                               u.Role == UserRole.Admin || 
                               u.Role == UserRole.SuperAdmin) && u.IsActive)
                    .ToListAsync();

                foreach (var approver in approvers)
                {
                    var notification = new Notification
                    {
                        Title = "任务审核通知",
                        Content = $"【{submitter?.FullName ?? submitter?.Username}】提交了任务【{task.Title}】待审核",
                        ReceiverId = approver.Id,
                        Type = NotificationType.Task,
                        RelatedId = task.Id,
                        RelatedType = "Task",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                // 通过SignalR推送实时通知给所有审批人
                foreach (var approver in approvers)
                {
                    var connectionId = NotificationHub.GetConnectionId(approver.Id);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        var notificationMessage = new
                        {
                            title = "任务审核通知",
                            content = $"【{submitter?.FullName ?? submitter?.Username}】提交了任务【{task.Title}】待审核",
                            type = NotificationType.Task.ToString(),
                            relatedId = task.Id,
                            relatedType = "Task",
                            createdAt = DateTime.UtcNow
                        };
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationMessage);
                    }
                }
            }
            catch
            {
                // 通知发送失败不影响主流程
            }
        }

        // 创建任务审批结果通知
        private async Task CreateTaskApprovalNotificationAsync(TodoTask task, int approverId, bool isApproved)
        {
            try
            {
                // 获取审批人信息
                var approver = await _context.Users.FindAsync(approverId);
                
                var notification = new Notification
                {
                    Title = "任务审批结果通知",
                    Content = $"您的任务【{task.Title}】已被{(isApproved ? "通过" : "驳回")}，审批人：【{approver?.FullName ?? approver?.Username}】",
                    ReceiverId = task.UserId,
                    Type = NotificationType.Task,
                    RelatedId = task.Id,
                    RelatedType = "Task",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 通过SignalR推送实时通知给任务创建者
                var connectionId = NotificationHub.GetConnectionId(task.UserId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    var notificationMessage = new
                    {
                        title = "任务审批结果通知",
                        content = $"您的任务【{task.Title}】已被{(isApproved ? "通过" : "驳回")}，审批人：【{approver?.FullName ?? approver?.Username}】",
                        type = NotificationType.Task.ToString(),
                        relatedId = task.Id,
                        relatedType = "Task",
                        createdAt = DateTime.UtcNow
                    };
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationMessage);
                }
            }
            catch
            {
                // 通知发送失败不影响主流程
            }
        }

        /// <summary>
        /// 获取任务统计报表（管理层视角）
        /// </summary>
        public async Task<ApiResult<TaskStatisticsDto>> GetTaskStatisticsAsync(int currentUserId, bool isAdmin)
        {
            // 权限校验：只有管理员可以查看统计报表
            if (!isAdmin)
            {
                return ApiResult<TaskStatisticsDto>.Fail("无权查看统计报表", 403);
            }

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // 1. 本月任务统计
            var monthlyTasks = await _context.Tasks
                .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                .ToListAsync();

            var monthlyCompletedCount = monthlyTasks.Count(t => t.Status == myTaskStatus.Completed);
            var monthlyTotalCount = monthlyTasks.Count;
            var monthlyCompletionRate = monthlyTotalCount > 0 
                ? Math.Round((decimal)monthlyCompletedCount / monthlyTotalCount * 100, 2) 
                : 0;

            // 2. 逾期任务统计
            var allTasks = await _context.Tasks
                .Where(t => t.Status != myTaskStatus.Completed && t.DueDate < now)
                .CountAsync();

            var totalActiveTasks = await _context.Tasks
                .Where(t => t.Status != myTaskStatus.Completed)
                .CountAsync();

            var overdueRate = totalActiveTasks > 0 
                ? Math.Round((decimal)allTasks / totalActiveTasks * 100, 2) 
                : 0;

            // 3. 各部门任务饱和度
            var departments = await _context.Departments
                .Include(d => d.Users)
                .ToListAsync();

            var departmentSaturation = new List<DepartmentTaskSaturationDto>();

            foreach (var dept in departments)
            {
                var deptUsers = dept.Users?.Select(u => u.Id).ToList() ?? new List<int>();
                
                var deptTasks = await _context.Tasks
                    .Where(t => deptUsers.Contains(t.UserId) && t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                    .ToListAsync();

                var completedCount = deptTasks.Count(t => t.Status == myTaskStatus.Completed);
                var inProgressCount = deptTasks.Count(t => t.Status == myTaskStatus.Processing);
                var overdueCount = deptTasks.Count(t => t.Status != myTaskStatus.Completed && t.DueDate < now);

                var totalMembers = dept.Users?.Count ?? 0;
                var saturationRate = totalMembers > 0 
                    ? Math.Round((decimal)deptTasks.Count / totalMembers, 2) 
                    : 0;

                var completionRate = deptTasks.Count > 0 
                    ? Math.Round((decimal)completedCount / deptTasks.Count * 100, 2) 
                    : 0;

                departmentSaturation.Add(new DepartmentTaskSaturationDto
                {
                    DepartmentId = dept.Id,
                    DepartmentName = dept.Name,
                    TotalMembers = totalMembers,
                    TaskCount = deptTasks.Count,
                    CompletedCount = completedCount,
                    InProgressCount = inProgressCount,
                    OverdueCount = overdueCount,
                    SaturationRate = saturationRate,
                    CompletionRate = completionRate
                });
            }

            var result = new TaskStatisticsDto
            {
                MonthlyCompletedCount = monthlyCompletedCount,
                MonthlyTotalCount = monthlyTotalCount,
                MonthlyCompletionRate = monthlyCompletionRate,
                OverdueCount = allTasks,
                OverdueRate = overdueRate,
                DepartmentSaturation = departmentSaturation
            };

            return ApiResult<TaskStatisticsDto>.Ok(result, "获取统计报表成功");
        }

        /// <summary>
        /// 获取任务趋势数据（按月份统计）
        /// </summary>
        public async Task<ApiResult<TaskTrendDto>> GetTaskTrendAsync(int currentUserId, bool isAdmin, int months = 12)
        {
            // 权限校验：只有管理员可以查看趋势数据
            if (!isAdmin)
            {
                return ApiResult<TaskTrendDto>.Fail("无权查看趋势数据", 403);
            }

            var now = DateTime.UtcNow;
            var trendData = new List<TaskTrendItemDto>();

            // 获取最近N个月的数据
            for (int i = months - 1; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // 当月创建的任务
                var createdTasks = await _context.Tasks
                    .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                    .ToListAsync();

                // 当月完成的任务
                var completedTasks = await _context.Tasks
                    .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd && t.Status == myTaskStatus.Completed)
                    .ToListAsync();

                // 当月逾期的任务
                var overdueTasks = await _context.Tasks
                    .Where(t => t.DueDate >= monthStart && t.DueDate <= monthEnd && t.DueDate < now && t.Status != myTaskStatus.Completed)
                    .ToListAsync();

                var createdCount = createdTasks.Count;
                var completedCount = completedTasks.Count;
                var overdueCount = overdueTasks.Count;

                var completionRate = createdCount > 0 
                    ? Math.Round((decimal)completedCount / createdCount * 100, 2) 
                    : 0;

                var overdueRate = createdCount > 0 
                    ? Math.Round((decimal)overdueCount / createdCount * 100, 2) 
                    : 0;

                trendData.Add(new TaskTrendItemDto
                {
                    Year = targetMonth.Year,
                    Month = targetMonth.Month,
                    MonthLabel = $"{targetMonth:yyyy-MM}",
                    CreatedCount = createdCount,
                    CompletedCount = completedCount,
                    OverdueCount = overdueCount,
                    CompletionRate = completionRate,
                    OverdueRate = overdueRate
                });
            }

            var result = new TaskTrendDto
            {
                TrendData = trendData
            };

            return ApiResult<TaskTrendDto>.Ok(result, "获取趋势数据成功");
        }

        /// <summary>
        /// 获取首页仪表盘数据 - 根据角色返回不同范围的数据
        /// </summary>
        public async Task<ApiResult<DashboardDto>> GetDashboardAsync(int currentUserId)
        {
            var now = DateTime.UtcNow;

            // 获取当前用户信息
            var currentUser = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                return ApiResult<DashboardDto>.Fail("用户不存在");
            }

            var result = new DashboardDto();

            // ========== 1. 个人数据（所有角色可见） ==========
            await LoadPersonalDataAsync(result, currentUserId, now);

            // ========== 2. 根据角色加载不同范围的数据 ==========
            var role = currentUser.Role;

            // 判断是否为管理员或超级管理员
            var isAdmin = role == UserRole.Admin || role == UserRole.SuperAdmin;

            // 判断是否为部门经理（包括Admin和SuperAdmin，因为他们也能看部门数据）
            var isManager = role == UserRole.DepartmentManager || role == UserRole.TeamLeader || isAdmin;

            // 加载部门数据
            if (isManager && currentUser.DepartmentId.HasValue)
            {
                result.ShowDepartmentData = true;
                await LoadDepartmentDataAsync(result, currentUser.DepartmentId.Value, now);
            }

            // 加载全局数据
            if (isAdmin)
            {
                result.ShowGlobalData = true;
                await LoadGlobalDataAsync(result, now);
            }

            // ========== 3. 最近动态（所有角色可见） ==========
            await LoadRecentActivitiesAsync(result, currentUserId, now);

            return ApiResult<DashboardDto>.Ok(result, "获取仪表盘数据成功");
        }

        /// <summary>
        /// 加载个人数据
        /// </summary>
        private async Task LoadPersonalDataAsync(DashboardDto result, int userId, DateTime now)
        {
            // 我的待办任务
            var myPendingTasks = await _context.Tasks
                .Where(t => t.AssignedUserId == userId &&
                       (t.Status == myTaskStatus.Todo || t.Status == myTaskStatus.Processing || t.Status == myTaskStatus.Review))
                .CountAsync();

            // 我的已完成任务
            var myCompletedTasks = await _context.Tasks
                .Where(t => t.AssignedUserId == userId && t.Status == myTaskStatus.Completed)
                .CountAsync();

            // 我的任务总数
            var myTotalTasks = await _context.Tasks
                .Where(t => t.AssignedUserId == userId)
                .CountAsync();

            // 我的申请总数
            var myApplications = await _context.WorkflowInstances
                .Where(w => w.CreatorId == userId)
                .CountAsync();

            // 我的未读消息
            var myUnreadNotifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .CountAsync();

            // 我的任务完成率
            var myCompletionRate = myTotalTasks > 0
                ? Math.Round((decimal)myCompletedTasks / myTotalTasks * 100, 2)
                : 0;

            result.MyPendingTasks = myPendingTasks;
            result.MyCompletedTasks = myCompletedTasks;
            result.MyApplications = myApplications;
            result.MyUnreadNotifications = myUnreadNotifications;
            result.MyCompletionRate = myCompletionRate;
        }

        /// <summary>
        /// 加载部门数据
        /// </summary>
        private async Task LoadDepartmentDataAsync(DashboardDto result, int departmentId, DateTime now)
        {
            // 获取部门信息
            var department = await _context.Departments
                .Include(d => d.Users)
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (department == null) return;

            result.DepartmentName = department.Name;
            result.DepartmentMemberCount = department.Users?.Count ?? 0;

            // 部门成员ID列表
            var memberIds = department.Users?.Select(u => u.Id).ToList() ?? new List<int>();

            // 部门待办任务
            var deptPendingTasks = await _context.Tasks
                .Where(t => memberIds.Contains(t.AssignedUserId ?? 0) &&
                       (t.Status == myTaskStatus.Todo || t.Status == myTaskStatus.Processing || t.Status == myTaskStatus.Review))
                .CountAsync();

            // 部门已完成任务
            var deptCompletedTasks = await _context.Tasks
                .Where(t => memberIds.Contains(t.AssignedUserId ?? 0) && t.Status == myTaskStatus.Completed)
                .CountAsync();

            // 部门任务总数
            var deptTotalTasks = await _context.Tasks
                .Where(t => memberIds.Contains(t.AssignedUserId ?? 0))
                .CountAsync();

            // 部门任务完成率
            var deptCompletionRate = deptTotalTasks > 0
                ? Math.Round((decimal)deptCompletedTasks / deptTotalTasks * 100, 2)
                : 0;

            result.DepartmentPendingTasks = deptPendingTasks;
            result.DepartmentCompletedTasks = deptCompletedTasks;
            result.DepartmentCompletionRate = deptCompletionRate;

            // 部门成员任务完成率
            var members = await _context.Users
                .Where(u => u.DepartmentId == departmentId)
                .Select(u => new MemberTaskDto
                {
                    UserId = u.Id,
                    UserName = u.FullName ?? u.Username,
                    TotalTasks = _context.Tasks.Count(t => t.AssignedUserId == u.Id),
                    CompletedTasks = _context.Tasks.Count(t => t.AssignedUserId == u.Id && t.Status == myTaskStatus.Completed)
                })
                .ToListAsync();

            members.ForEach(m =>
            {
                m.CompletionRate = m.TotalTasks > 0
                    ? Math.Round((decimal)m.CompletedTasks / m.TotalTasks * 100, 2)
                    : 0;
            });

            result.DepartmentMembers = members;

            // 部门任务趋势（近6个月）
            result.DepartmentTrend = await GetMonthlyTrendAsync(memberIds, 6, now);
        }

        /// <summary>
        /// 加载全局数据
        /// </summary>
        private async Task LoadGlobalDataAsync(DashboardDto result, DateTime now)
        {
            // 全公司任务总数
            var globalTotalTasks = await _context.Tasks.CountAsync();

            // 全公司待办任务
            var globalPendingTasks = await _context.Tasks
                .Where(t => t.Status == myTaskStatus.Todo || t.Status == myTaskStatus.Processing || t.Status == myTaskStatus.Review)
                .CountAsync();

            // 全公司已完成任务
            var globalCompletedTasks = await _context.Tasks
                .Where(t => t.Status == myTaskStatus.Completed)
                .CountAsync();

            // 全公司逾期任务
            var globalOverdueTasks = await _context.Tasks
                .Where(t => t.DueDate < now && t.Status != myTaskStatus.Completed)
                .CountAsync();

            // 全公司任务完成率
            var globalCompletionRate = globalTotalTasks > 0
                ? Math.Round((decimal)globalCompletedTasks / globalTotalTasks * 100, 2)
                : 0;

            // 全公司逾期率
            var globalOverdueRate = globalTotalTasks > 0
                ? Math.Round((decimal)globalOverdueTasks / globalTotalTasks * 100, 2)
                : 0;

            result.GlobalTotalTasks = globalTotalTasks;
            result.GlobalPendingTasks = globalPendingTasks;
            result.GlobalCompletedTasks = globalCompletedTasks;
            result.GlobalOverdueTasks = globalOverdueTasks;
            result.GlobalCompletionRate = globalCompletionRate;
            result.GlobalOverdueRate = globalOverdueRate;

            // 各部门任务统计
            var departments = await _context.Departments
                .Include(d => d.Users)
                .ToListAsync();

            var deptStats = new List<DepartmentTaskDto>();

            foreach (var dept in departments)
            {
                var memberIds = dept.Users?.Select(u => u.Id).ToList() ?? new List<int>();

                if (!memberIds.Any()) continue;

                var totalTasks = await _context.Tasks
                    .Where(t => memberIds.Contains(t.AssignedUserId ?? 0))
                    .CountAsync();

                var pendingTasks = await _context.Tasks
                    .Where(t => memberIds.Contains(t.AssignedUserId ?? 0) &&
                           (t.Status == myTaskStatus.Todo || t.Status == myTaskStatus.Processing || t.Status == myTaskStatus.Review))
                    .CountAsync();

                var completedTasks = await _context.Tasks
                    .Where(t => memberIds.Contains(t.AssignedUserId ?? 0) && t.Status == myTaskStatus.Completed)
                    .CountAsync();

                var overdueTasks = await _context.Tasks
                    .Where(t => memberIds.Contains(t.AssignedUserId ?? 0) && t.DueDate < now && t.Status != myTaskStatus.Completed)
                    .CountAsync();

                var completionRate = totalTasks > 0
                    ? Math.Round((decimal)completedTasks / totalTasks * 100, 2)
                    : 0;

                deptStats.Add(new DepartmentTaskDto
                {
                    DepartmentId = dept.Id,
                    DepartmentName = dept.Name,
                    TotalTasks = totalTasks,
                    PendingTasks = pendingTasks,
                    CompletedTasks = completedTasks,
                    OverdueTasks = overdueTasks,
                    MemberCount = memberIds.Count,
                    CompletionRate = completionRate
                });
            }

            result.DepartmentStats = deptStats;

            // 全公司任务趋势（近12个月）
            result.GlobalTrend = await GetMonthlyTrendAsync(null, 12, now);
        }

        /// <summary>
        /// 加载最近动态
        /// </summary>
        private async Task LoadRecentActivitiesAsync(DashboardDto result, int userId, DateTime now)
        {
            var recentLogs = await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            result.RecentActivities = recentLogs.Select((log, index) => new ActivityDto
            {
                Id = index + 1,
                Title = log.OperationType,
                Time = GetTimeAgo(log.CreatedAt, now)
            }).ToList();
        }

        /// <summary>
        /// 获取月度趋势数据
        /// </summary>
        private async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(List<int>? userIds, int months, DateTime now)
        {
            var trend = new List<MonthlyTrendDto>();

            for (int i = months - 1; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // 创建任务数
                var createdQuery = _context.Tasks.Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd);
                var completedQuery = _context.Tasks.Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd && t.Status == myTaskStatus.Completed);
                var overdueQuery = _context.Tasks.Where(t => t.DueDate >= monthStart && t.DueDate <= monthEnd && t.DueDate < now && t.Status != myTaskStatus.Completed);

                // 如果指定了用户ID列表，则过滤
                if (userIds != null && userIds.Any())
                {
                    createdQuery = createdQuery.Where(t => userIds.Contains(t.AssignedUserId ?? 0));
                    completedQuery = completedQuery.Where(t => userIds.Contains(t.AssignedUserId ?? 0));
                    overdueQuery = overdueQuery.Where(t => userIds.Contains(t.AssignedUserId ?? 0));
                }

                var createdCount = await createdQuery.CountAsync();
                var completedCount = await completedQuery.CountAsync();
                var overdueCount = await overdueQuery.CountAsync();

                trend.Add(new MonthlyTrendDto
                {
                    MonthLabel = $"{targetMonth:yyyy-MM}",
                    CreatedCount = createdCount,
                    CompletedCount = completedCount,
                    OverdueCount = overdueCount
                });
            }

            return trend;
        }

        /// <summary>
        /// 计算时间差描述
        /// </summary>
        private string GetTimeAgo(DateTime time, DateTime now)
        {
            var diff = now - time;

            if (diff.TotalMinutes < 1) return "刚刚";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}小时前";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}天前";
            return time.ToString("MM-dd HH:mm");
        }
    }
}
