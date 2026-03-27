using System;
using System.ComponentModel.DataAnnotations;
using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? DueDate { get; set; }

        // 新增字段
        public int CreatorId { get; set; }  // 创建者ID
        public string? CreatorName { get; set; }  // 创建者姓名

        public int? DepartmentId { get; set; }  // 指派部门ID
        public string? DepartmentName { get; set; }  // 指派部门名称
        public string? DepartmentFullPath { get; set; }  // 部门完整路径

        public int? AssignedUserId { get; set; }  // 指派用户ID
        public string? AssignedUserName { get; set; }  // 指派用户姓名

        // 计算属性
        public string StatusName => Status;
        public string PriorityName => Priority;
        public bool IsAssignedToDepartment => DepartmentId.HasValue;
        public bool IsAssignedToUser => AssignedUserId.HasValue;
    }

    //public class UpdateTaskDto
    //{
    //    [MaxLength(200)]
    //    public string? Title { get; set; }

    //    [MaxLength(1000)]
    //    public string? Description { get; set; }

    //    public TaskPriority? Priority { get; set; }

    //    public DateTime? DueDate { get; set; }

    //    // 新增字段
    //    public int? DepartmentId { get; set; }
    //    public int? AssignedUserId { get; set; }
    //}

    public class AssignTaskDto
    {
        public int? DepartmentId { get; set; }
        public int? AssignedUserId { get; set; }
        public string? Remarks { get; set; }
    }
}
