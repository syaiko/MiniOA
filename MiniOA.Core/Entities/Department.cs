using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? ParentId { get; set; }  // 父部门ID，null表示根部门

        [Required]
        public int ManagerId { get; set; }  // 部门负责人

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // 导航属性
        [ForeignKey("ParentId")]
        public virtual Department? Parent { get; set; }

        public virtual ICollection<Department> Children { get; set; } = new List<Department>();

        public virtual User Manager { get; set; } = null!;

        [JsonIgnore]
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        [JsonIgnore]
        public virtual ICollection<WorkflowNode> WorkflowNodes { get; set; } = new List<WorkflowNode>();

        public virtual ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();

        // 计算属性：部门层级
        [NotMapped]
        public int Level => Parent?.Level + 1 ?? 0;

        // 计算属性：完整路径
        [NotMapped]
        public string FullPath => Parent != null ? $"{Parent.FullPath} > {Name}" : Name;
    }
}
