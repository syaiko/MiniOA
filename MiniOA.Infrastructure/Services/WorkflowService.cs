using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Infrastructure.Hubs;
using System.Text.Json;

namespace MiniOA.Infrastructure.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public WorkflowService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 提交流程
        public async Task<ApiResult<WorkflowInstanceDto>> SubmitWorkflowAsync(int currentUserId, SubmitWorkflowInputDto input)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. 创建流程实例
                var instance = new WorkflowInstance
                {
                    Title = input.Title,
                    WorkflowType = input.WorkflowType,
                    CreatorId = currentUserId,
                    Status = WorkflowStatus.Pending,
                    BusinessData = input.BusinessData,
                    CreatedAt = DateTime.UtcNow
                };

                // 2. 自动找到第一个节点（序号最小）
                var firstNode = await _context.WorkflowNodes
                    .Where(n => n.WorkflowType == input.WorkflowType && n.IsActive)
                    .OrderBy(n => n.OrderIndex)
                    .FirstOrDefaultAsync();

                if (firstNode != null)
                {
                    instance.CurrentNodeId = firstNode.Id;
                    instance.Status = WorkflowStatus.InProgress;
                }

                _context.WorkflowInstances.Add(instance);
                await _context.SaveChangesAsync();

                // 3. 创建审批记录（提交动作）
                if (firstNode != null)
                {
                    var record = new ApprovalRecord
                    {
                        WorkflowInstanceId = instance.Id,
                        NodeId = firstNode.Id,
                        ApproverId = currentUserId,
                        Action = ApprovalAction.Submit,
                        Comment = "提交流程",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ApprovalRecords.Add(record);
                    await _context.SaveChangesAsync();
                }

                // 4. 创建第一条通知（给经理）
                await CreateSubmitNotificationAsync(instance, firstNode);

                await transaction.CommitAsync();

                return await GetInstanceDtoAsync(instance.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<WorkflowInstanceDto>.Fail($"提交流程失败: {ex.Message}", 500);
            }
        }

        // 重新提交流程（更新已退回的流程实例）
        public async Task<ApiResult<WorkflowInstanceDto>> ResubmitWorkflowAsync(int currentUserId, int instanceId, SubmitWorkflowInputDto input)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. 获取原流程实例
                var instance = await _context.WorkflowInstances.FindAsync(instanceId);
                if (instance == null)
                    return ApiResult<WorkflowInstanceDto>.Fail("流程实例不存在", 404);

                // 2. 验证权限（只有创建者可以重新提交）
                if (instance.CreatorId != currentUserId)
                    return ApiResult<WorkflowInstanceDto>.Fail("无权操作此流程", 403);

                // 3. 验证状态（只有已退回的流程可以重新提交）
                if (instance.Status != WorkflowStatus.Returned)
                    return ApiResult<WorkflowInstanceDto>.Fail("只有已退回的流程可以重新提交", 400);

                // 4. 更新流程实例
                instance.Title = input.Title;
                instance.BusinessData = input.BusinessData;
                instance.Status = WorkflowStatus.InProgress;
                instance.CompletedAt = null;

                // 5. 找到第一个节点
                var firstNode = await _context.WorkflowNodes
                    .Where(n => n.WorkflowType == instance.WorkflowType && n.IsActive)
                    .OrderBy(n => n.OrderIndex)
                    .FirstOrDefaultAsync();

                if (firstNode != null)
                {
                    instance.CurrentNodeId = firstNode.Id;
                }

                await _context.SaveChangesAsync();

                // 6. 创建重新提交的审批记录
                if (firstNode != null)
                {
                    var record = new ApprovalRecord
                    {
                        WorkflowInstanceId = instance.Id,
                        NodeId = firstNode.Id,
                        ApproverId = currentUserId,
                        Action = ApprovalAction.Submit,
                        Comment = "重新提交",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ApprovalRecords.Add(record);
                    await _context.SaveChangesAsync();
                }

                // 7. 创建通知（给审批人）
                await CreateSubmitNotificationAsync(instance, firstNode);

                await transaction.CommitAsync();

                return await GetInstanceDtoAsync(instance.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<WorkflowInstanceDto>.Fail($"重新提交失败: {ex.Message}", 500);
            }
        }

        // 创建提交通知
        private async Task CreateSubmitNotificationAsync(WorkflowInstance instance, WorkflowNode? node)
        {
            if (node == null) return;

            // 获取审批人
            int? receiverId = null;
            
            if (node.ApproverId.HasValue)
            {
                receiverId = node.ApproverId.Value;
            }
            else if (node.DepartmentId.HasValue)
            {
                // 获取部门经理
                var department = await _context.Departments
                    .Include(d => d.Manager)
                    .FirstOrDefaultAsync(d => d.Id == node.DepartmentId.Value);
                
                if (department?.ManagerId != null)
                {
                    receiverId = department.ManagerId;
                }
            }

            // 创建审批人通知
            if (receiverId.HasValue)
            {
                var notification = new Notification
                {
                    Title = "新的审批任务",
                    Content = $"您有一个新的审批任务：【{instance.Title}】",
                    ReceiverId = receiverId.Value,
                    Type = NotificationType.Workflow,
                    RelatedId = instance.Id,
                    RelatedType = "WorkflowInstance",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 通过SignalR推送实时通知
                var connectionId = NotificationHub.GetConnectionId(receiverId.Value);
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

            // 处理抄送逻辑
            await CreateCcNotificationsAsync(instance, node);
        }

        // 创建抄送通知
        private async Task CreateCcNotificationsAsync(WorkflowInstance instance, WorkflowNode node)
        {
            try
            {
                // 解析业务数据，检查金额条件
                var data = string.IsNullOrEmpty(instance.BusinessData) 
                    ? new Dictionary<string, object>() 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(instance.BusinessData);

                if (data == null) return;

                // 检查是否有金额字段且大于5000
                bool shouldCcToManager = false;
                if (data.TryGetValue("amount", out var amountValue))
                {
                    var amount = Convert.ToDecimal(amountValue.ToString());
                    shouldCcToManager = amount > 5000;
                }

                if (shouldCcToManager)
                {
                    // 获取申请人信息
                    var creator = await _context.Users
                        .Include(u => u.Department)
                        .FirstOrDefaultAsync(u => u.Id == instance.CreatorId);
                    
                    if (creator?.DepartmentId != null)
                    {
                        // 查找申请人所在部门的部门经理
                        var manager = await _context.Departments
                            .Include(d => d.Manager)
                            .FirstOrDefaultAsync(d => d.Id == creator.DepartmentId.Value && d.ManagerId != null);
                        
                        if (manager?.Manager != null)
                        {
                            var ccNotification = new Notification
                            {
                                Title = "工作流抄送通知",
                                Content = $"【{instance.Title}】申请金额超过5000元，已抄送给您审阅",
                                ReceiverId = manager.Manager.Id,
                                Type = NotificationType.WorkflowCc,
                                RelatedId = instance.Id,
                                RelatedType = "WorkflowInstance",
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.Notifications.Add(ccNotification);
                            await _context.SaveChangesAsync();

                            // 通过SignalR推送实时通知
                            var connectionId = NotificationHub.GetConnectionId(manager.Manager.Id);
                            if (!string.IsNullOrEmpty(connectionId))
                            {
                                var notificationMessage = new
                                {
                                    id = ccNotification.Id,
                                    title = ccNotification.Title,
                                    content = ccNotification.Content,
                                    type = ccNotification.Type.ToString(),
                                    relatedId = ccNotification.RelatedId,
                                    relatedType = ccNotification.RelatedType,
                                    createdAt = ccNotification.CreatedAt
                                };
                                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationMessage);
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(node.CcUserIds))
                {
                    // 处理节点配置的抄送人员
                    var ccUserIds = JsonSerializer.Deserialize<List<int>>(node.CcUserIds);
                    if (ccUserIds != null && ccUserIds.Any())
                    {
                        var ccUsers = await _context.Users
                            .Where(u => ccUserIds.Contains(u.Id) && u.IsActive)
                            .ToListAsync();

                        foreach (var ccUser in ccUsers)
                        {
                            var ccNotification = new Notification
                            {
                                Title = "工作流抄送通知",
                                Content = $"【{instance.Title}】已抄送给您审阅",
                                ReceiverId = ccUser.Id,
                                Type = NotificationType.WorkflowCc,
                                RelatedId = instance.Id,
                                RelatedType = "WorkflowInstance",
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.Notifications.Add(ccNotification);
                        }

                        await _context.SaveChangesAsync();

                        // 通过SignalR推送实时通知给所有抄送人员
                        foreach (var ccUser in ccUsers)
                        {
                            var connectionId = NotificationHub.GetConnectionId(ccUser.Id);
                            if (!string.IsNullOrEmpty(connectionId))
                            {
                                var notificationMessage = new
                                {
                                    title = "工作流抄送通知",
                                    content = $"【{instance.Title}】已抄送给您审阅",
                                    type = NotificationType.WorkflowCc.ToString(),
                                    relatedId = instance.Id,
                                    relatedType = "WorkflowInstance",
                                    createdAt = DateTime.UtcNow
                                };
                                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationMessage);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                // 抄送失败不影响主流程
            }
        }

        public async Task<ApiResult<WorkflowInstanceDto>> ApproveAsync(int currentUserId, ApproveInputDto input)
        {
            Console.WriteLine($"开始审批 - 用户ID: {currentUserId}, 实例ID: {input.InstanceId}, 动作: {input.Action}");
            
            // 使用事务
            await using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. 获取流程实例
                var instance = await _context.WorkflowInstances
                    .Include(w => w.CurrentNode)
                    .FirstOrDefaultAsync(w => w.Id == input.InstanceId);

                if (instance == null)
                    return ApiResult<WorkflowInstanceDto>.Fail("流程实例不存在", 404);

                Console.WriteLine($"流程实例 - ID: {instance.Id}, 类型: {instance.WorkflowType}, 当前节点ID: {instance.CurrentNodeId}");

                if (instance.Status == WorkflowStatus.Approved ||
                    instance.Status == WorkflowStatus.Rejected ||
                    instance.Status == WorkflowStatus.Cancelled ||
                    instance.Status == WorkflowStatus.Returned)
                    return ApiResult<WorkflowInstanceDto>.Fail("流程已结束", 400);

                // 2. 检查当前节点，如果为空则获取第一个节点
                WorkflowNode? currentNode;
                if (instance.CurrentNodeId.HasValue)
                {
                    currentNode = instance.CurrentNode;
                    Console.WriteLine($"使用现有节点 - ID: {currentNode?.Id}, 名称: {currentNode?.Name}");
                }
                else
                {
                    // 获取第一个节点作为当前节点
                    Console.WriteLine($"查找第一个节点 - 流程类型: {instance.WorkflowType}");
                    currentNode = await _context.WorkflowNodes
                        .Where(n => n.WorkflowType == instance.WorkflowType && n.IsActive)
                        .OrderBy(n => n.OrderIndex)
                        .FirstOrDefaultAsync();
                    
                    Console.WriteLine($"找到第一个节点 - ID: {currentNode?.Id}, 名称: {currentNode?.Name}");
                    
                    if (currentNode == null)
                        return ApiResult<WorkflowInstanceDto>.Fail("当前节点不存在", 404);
                    
                    // 更新实例的当前节点
                    instance.CurrentNodeId = currentNode.Id;
                    instance.Status = WorkflowStatus.InProgress;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"更新实例当前节点为: {currentNode.Id}");
                }

                // 3. 校验审批权限
                var permissionResult = await CheckApprovePermission(currentUserId, instance, currentNode);
                Console.WriteLine($"权限校验结果: {permissionResult.Success}, 消息: {permissionResult.Message}");
                if (!permissionResult.Success)
                    return ApiResult<WorkflowInstanceDto>.Fail(permissionResult.Message, permissionResult.Code);

                // 3. 插入审批记录
                var record = new ApprovalRecord
                {
                    WorkflowInstanceId = instance.Id,
                    NodeId = currentNode.Id,
                    ApproverId = currentUserId,
                    Action = input.Action,
                    Comment = input.Comment,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ApprovalRecords.Add(record);

                // 4. 根据动作更新流程状态
                Console.WriteLine($"审批动作: {input.Action}, 当前状态: {instance.Status}");
                
                if (input.Action == ApprovalAction.Reject)
                {
                    instance.Status = WorkflowStatus.Rejected;
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.CurrentNodeId = null;
                    Console.WriteLine($"状态更新为: Rejected");
                }
                else if (input.Action == ApprovalAction.Approve)
                {
                    // 5. 根据条件表达式决定下一节点
                    var nextNode = await GetNextNodeAsync(currentNode, instance.BusinessData);
                    Console.WriteLine($"找到下一节点: {nextNode?.Id} - {nextNode?.Name}");
                    
                    if (nextNode == null)
                    {
                        // 流程结束
                        instance.Status = WorkflowStatus.Approved;
                        instance.CompletedAt = DateTime.UtcNow;
                        instance.CurrentNodeId = null;
                        Console.WriteLine($"流程结束，状态更新为: Approved");
                    }
                    else
                    {
                        // 流转到下一节点
                        instance.CurrentNodeId = nextNode.Id;
                        instance.Status = WorkflowStatus.InProgress;
                        Console.WriteLine($"流转到下一节点: {nextNode.Id}，状态保持: InProgress");
                    }
                }
                else if (input.Action == ApprovalAction.Return)
                {
                    // 退回到发起人
                    instance.Status = WorkflowStatus.Returned;
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.CurrentNodeId = null;
                    Console.WriteLine($"状态更新为: Returned（退回）");
                }

                Console.WriteLine($"最终状态: {instance.Status}, 当前节点ID: {instance.CurrentNodeId}");

                await _context.SaveChangesAsync();

                // 6. 生成通知
                await CreateNotificationAsync(instance, input.Action, currentUserId);

                // 7. 处理下一节点的抄送
                if (input.Action == ApprovalAction.Approve && instance.Status == WorkflowStatus.InProgress)
                {
                    var nextNode = await GetNextNodeAsync(currentNode, instance.BusinessData);
                    if (nextNode != null)
                    {
                        await CreateCcNotificationsAsync(instance, nextNode);
                    }
                }

                await transaction.CommitAsync();

                // 返回结果
                return await GetInstanceDtoAsync(instance.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<WorkflowInstanceDto>.Fail($"审批失败: {ex.Message}", 500);
            }
        }

        // 校验是否是处理人
        private async Task<bool> IsApproverAsync(int userId, WorkflowNode node)
        {
            // 获取用户信息
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // 超级管理员和管理员可以审批所有流程
            if (user.Role == UserRole.SuperAdmin || user.Role == UserRole.Admin)
                return true;

            // 指定审批人
            if (node.ApproverId.HasValue)
                return node.ApproverId.Value == userId;

            // 部门审批人 - 部门经理可以审批本部门的申请
            if (node.DepartmentId.HasValue)
            {
                // 部门经理可以审批本部门的所有申请
                if (user.Role == UserRole.DepartmentManager && user.DepartmentId == node.DepartmentId.Value)
                    return true;
                
                // 普通用户只能审批指定给自己的
                return user.DepartmentId == node.DepartmentId.Value;
            }

            return false;
        }

        // 校验审批权限（返回ApiResult）
        private async Task<ApiResult<bool>> CheckApprovePermission(int userId, WorkflowInstance instance, WorkflowNode currentNode)
        {
            if (currentNode == null)
                return ApiResult<bool>.Fail("当前节点不存在", 404);

            var hasPermission = await IsApproverAsync(userId, currentNode);
            
            if (!hasPermission)
                return ApiResult<bool>.Fail("您不是当前节点的处理人", 403);

            return ApiResult<bool>.Ok(true);
        }

        // 根据条件表达式获取下一节点
        private async Task<WorkflowNode?> GetNextNodeAsync(WorkflowNode currentNode, string? businessData)
        {
            // 如果有下一节点ID，直接返回
            if (currentNode.NextNodeId.HasValue)
            {
                return await _context.WorkflowNodes.FindAsync(currentNode.NextNodeId.Value);
            }

            // 如果有条件表达式，解析并计算
            if (!string.IsNullOrEmpty(currentNode.ConditionExpression))
            {
                return await EvaluateConditionAsync(currentNode.ConditionExpression, businessData);
            }

            return null;
        }

        // 计算条件表达式
        private async Task<WorkflowNode?> EvaluateConditionAsync(string expression, string? businessData)
        {
            try
            {
                // 解析业务数据
                var data = string.IsNullOrEmpty(businessData) 
                    ? new Dictionary<string, object>() 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(businessData);

                if (data == null) return null;

                // 简单条件判断示例: "amount > 5000"
                var parts = expression.Split(' ');
                if (parts.Length == 3)
                {
                    var fieldName = parts[0];
                    var op = parts[1];
                    var value = decimal.Parse(parts[2]);

                    if (data.TryGetValue(fieldName, out var fieldValue))
                    {
                        var numValue = Convert.ToDecimal(fieldValue.ToString());
                        var result = op switch
                        {
                            ">" => numValue > value,
                            ">=" => numValue >= value,
                            "<" => numValue < value,
                            "<=" => numValue <= value,
                            "==" => numValue == value,
                            "!=" => numValue != value,
                            _ => false
                        };

                        // 根据结果查找对应节点
                        var targetNodeType = result ? "Approved" : "Rejected";
                        return await _context.WorkflowNodes
                            .FirstOrDefaultAsync(n => n.NodeType == targetNodeType && n.IsActive);
                    }
                }
            }
            catch
            {
                // 条件解析失败，返回null
            }

            return null;
        }

        // 创建通知
        private async Task CreateNotificationAsync(WorkflowInstance instance, ApprovalAction action, int operatorId)
        {
            var notification = new Notification
            {
                Title = $"流程审批通知",
                Content = $"您的流程【{instance.Title}】已被{GetActionName(action)}",
                ReceiverId = instance.CreatorId,
                Type = NotificationType.Workflow,
                RelatedId = instance.Id,
                RelatedType = "WorkflowInstance",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 通过SignalR推送实时通知
            var connectionId = NotificationHub.GetConnectionId(instance.CreatorId);
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

        // 获取动作名称
        private string GetActionName(ApprovalAction action)
        {
            return action switch
            {
                ApprovalAction.Submit => "提交",
                ApprovalAction.Approve => "批准",
                ApprovalAction.Reject => "拒绝",
                ApprovalAction.Return => "退回",
                ApprovalAction.Cancel => "取消",
                _ => "处理"
            };
        }

        // 获取实例DTO
        private async Task<ApiResult<WorkflowInstanceDto>> GetInstanceDtoAsync(int id)
        {
            var instance = await _context.WorkflowInstances
                .Include(w => w.Creator)
                .Include(w => w.CurrentNode)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (instance == null)
                return ApiResult<WorkflowInstanceDto>.Fail("实例不存在", 404);

            var dto = new WorkflowInstanceDto
            {
                Id = instance.Id,
                Title = instance.Title,
                WorkflowType = instance.WorkflowType,
                CreatorId = instance.CreatorId,
                CreatorName = instance.Creator?.FullName ?? instance.Creator?.Username,
                CurrentNodeId = instance.CurrentNodeId,
                CurrentNodeName = instance.CurrentNode?.Name,
                Status = instance.Status,
                BusinessData = instance.BusinessData,
                CreatedAt = instance.CreatedAt,
                CompletedAt = instance.CompletedAt
            };

            return ApiResult<WorkflowInstanceDto>.Ok(dto);
        }

        public async Task<ApiResult<WorkflowInstanceDto>> GetInstanceAsync(int id)
        {
            return await GetInstanceDtoAsync(id);
        }

        public async Task<ApiResult<List<WorkflowInstanceDto>>> GetMyPendingAsync(int userId)
        {
            // 获取用户信息以判断角色和部门
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ApiResult<List<WorkflowInstanceDto>>.Fail("用户不存在", 404);

            // 基础查询：待处理或正在进行中的流程
            var query = _context.WorkflowInstances
                .Include(w => w.Creator)
                .Include(w => w.CurrentNode)
                .Where(w => (w.Status == WorkflowStatus.Pending || w.Status == WorkflowStatus.InProgress));

            // 权限过滤逻辑：
            // 1. 如果是超级管理员(0)或系统管理员(1)，可以看到所有待办
            // 2. 如果是部门经理(2)，可以看到本部门所有待办
            // 3. 如果是普通用户或组长，只能看到：
            //    - 直接指定审批人为自己的
            //    - 节点属于自己部门且未指定审批人的
            if (user.Role > UserRole.DepartmentManager)
            {
                query = query.Where(w => 
                    (w.CurrentNode!.ApproverId == userId) ||
                    (w.CurrentNode!.DepartmentId == user.DepartmentId && w.CurrentNode.ApproverId == null));
            }

            var instances = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();

            var dtos = instances.Select(w => new WorkflowInstanceDto
            {
                Id = w.Id,
                Title = w.Title,
                WorkflowType = w.WorkflowType,
                CreatorId = w.CreatorId,
                CreatorName = w.Creator?.FullName ?? w.Creator?.Username,
                CurrentNodeId = w.CurrentNodeId,
                CurrentNodeName = w.CurrentNode?.Name,
                Status = w.Status,
                BusinessData = w.BusinessData,
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            }).ToList();

            return ApiResult<List<WorkflowInstanceDto>>.Ok(dtos);
        }

        // 获取我的申请列表
        public async Task<ApiResult<List<WorkflowInstanceDto>>> GetMyInstancesAsync(int userId)
        {
            // 明确只查询发起人是当前用户的流程
            var instances = await _context.WorkflowInstances
                .Include(w => w.Creator)
                .Include(w => w.CurrentNode)
                .Where(w => w.CreatorId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var dtos = instances.Select(w => new WorkflowInstanceDto
            {
                Id = w.Id,
                Title = w.Title,
                WorkflowType = w.WorkflowType,
                CreatorId = w.CreatorId,
                CreatorName = w.Creator?.FullName ?? w.Creator?.Username,
                CurrentNodeId = w.CurrentNodeId,
                CurrentNodeName = w.CurrentNode?.Name,
                Status = w.Status,
                BusinessData = w.BusinessData,
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            }).ToList();

            return ApiResult<List<WorkflowInstanceDto>>.Ok(dtos);
        }

        // 获取流程节点列表
        public async Task<ApiResult<List<WorkflowNodeDto>>> GetWorkflowNodesAsync(string workflowType)
        {
            var nodes = await _context.WorkflowNodes
                .Include(n => n.Approver)
                .Include(n => n.Department)
                .Where(n => n.WorkflowType == workflowType && n.IsActive)
                .OrderBy(n => n.OrderIndex)
                .ToListAsync();

            var dtos = nodes.Select(n => new WorkflowNodeDto
            {
                Id = n.Id,
                Name = n.Name,
                NodeType = n.NodeType,
                OrderIndex = n.OrderIndex,
                ApproverId = n.ApproverId,
                ApproverName = n.Approver?.FullName ?? n.Approver?.Username,
                DepartmentId = n.DepartmentId,
                DepartmentName = n.Department?.Name,
                IsActive = n.IsActive
            }).ToList();

            return ApiResult<List<WorkflowNodeDto>>.Ok(dtos);
        }

        // 获取我的已办列表
        public async Task<ApiResult<List<WorkflowInstanceDto>>> GetMyDoneAsync(int userId)
        {
            // 获取用户已审批的流程
            var approvedInstanceIds = await _context.ApprovalRecords
                .Where(r => r.ApproverId == userId)
                .Select(r => r.WorkflowInstanceId)
                .Distinct()
                .ToListAsync();

            var instances = await _context.WorkflowInstances
                .Include(w => w.Creator)
                .Include(w => w.CurrentNode)
                .Where(w => approvedInstanceIds.Contains(w.Id))
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var dtos = instances.Select(w => new WorkflowInstanceDto
            {
                Id = w.Id,
                Title = w.Title,
                WorkflowType = w.WorkflowType,
                CreatorId = w.CreatorId,
                CreatorName = w.Creator?.FullName ?? w.Creator?.Username,
                CurrentNodeId = w.CurrentNodeId,
                CurrentNodeName = w.CurrentNode?.Name,
                Status = w.Status,
                BusinessData = w.BusinessData,
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            }).ToList();

            return ApiResult<List<WorkflowInstanceDto>>.Ok(dtos);
        }
    }
}
