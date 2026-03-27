using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;
using MiniOA.Core.Enums;

namespace MiniOA.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required,MaxLength(50)]
        public string Username { get; set; }

        [NotMapped]
        public string Password { get; set; }

        [Required]
        [ValidateNever]
        public byte[] PasswordHash { get; set; }

        [Required]
        [ValidateNever]
        public byte[] PasswordSalt { get; set; }

        public string? FullName { get; set; } 

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public UserRole Role { get; set; } = UserRole.Employee;

        public int? DepartmentId { get; set; }  // 所属部门

        public bool IsActive { get; set; } = true;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        // 导航属性

        [JsonIgnore]
        public virtual ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [JsonIgnore]
        public virtual ICollection<Department> ManagedDepartments { get; set; } = new List<Department>();

        [JsonIgnore]
        public virtual ICollection<TodoTask> AssignedTasks { get; set; } = new List<TodoTask>();

        [JsonIgnore]
        public virtual ICollection<WorkflowInstance> CreatedWorkflows { get; set; } = new List<WorkflowInstance>();

        [JsonIgnore]
        public virtual ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();

        [JsonIgnore]
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
