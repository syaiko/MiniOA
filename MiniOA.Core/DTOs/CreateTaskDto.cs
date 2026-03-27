using System.ComponentModel.DataAnnotations;
using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "标题是必填项")]
        [MaxLength(200,ErrorMessage = "标题长度不能超过200个字符")]
        [MinLength(2, ErrorMessage = "标题长度不能少于2个字符")]
        public string Title { get; set; }

        [MaxLength(1000, ErrorMessage = "描述长度不能超过1000个字符")]
        public string? Description { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        [DataType(DataType.DateTime)]
        [CustomValidation(typeof(CreateTaskDto), nameof(ValidateDueDate))]
        public DateTime? DueDate { get; set; }

        // 新增字段：部门和用户指派
        public int? DepartmentId { get; set; }  // 指派部门
        public int? AssignedUserId { get; set; }  // 指派用户

        public static ValidationResult ValidateDueDate(DateTime? dueDate, ValidationContext context)
        {
            if (dueDate.HasValue && dueDate.Value <= DateTime.UtcNow)
            {
                return new ValidationResult("截止日期必须晚于当前时间");
            }
            return ValidationResult.Success;
        }
    }
}
