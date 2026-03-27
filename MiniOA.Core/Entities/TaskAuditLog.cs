using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class TaskAuditLog
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }  // 任务ID

        [Required]
        public int UserId { get; set; }  // 操作用户ID

        [Required]
        public myTaskStatus FromStatus { get; set; }  // 原状态

        [Required]
        public myTaskStatus ToStatus { get; set; }    // 新状态

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 操作时间

        [Required]
        [MaxLength(100)]
        public string OperationType { get; set; }  // 操作类型：StatusChange, Create, Delete等

        [MaxLength(500)]
        public string? Remarks { get; set; }  // 备注信息

        // 导航属性
        [JsonIgnore]
        public virtual TodoTask Task { get; set; }
        
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
