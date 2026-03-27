using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;


namespace MiniOA.Core.Entities
{
    public class TodoTask
    {
        public int Id { get; set; }  // EF 默认识别 Id 为主键

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }  // .NET 8 required 关键字，防止空引用
        
        [MaxLength(1000)]
        public string? Description { get; set; }  // 任务描述
        public myTaskStatus Status { get; set; } = myTaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;  // 任务优先级
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; } // 允许为null，可选截止日期

        [Required]
        public int UserId { get; set; }  //创建任务的员工

        public int? DepartmentId { get; set; }  // 指派的部门（可选）

        public int? AssignedUserId { get; set; }  // 指派的具体用户（可选）

        [JsonIgnore]
        public virtual User User { get; set; }  // 导航属性，表示任务所属的员工

        // 新增导航属性
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [ForeignKey("AssignedUserId")]
        public virtual User? AssignedUser { get; set; }
    }
}
