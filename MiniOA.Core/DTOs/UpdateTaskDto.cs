using System.ComponentModel.DataAnnotations;
using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class UpdateTaskDto
    {
        [Required(ErrorMessage = "标题是必填项")]
        [MaxLength(200, ErrorMessage = "标题长度不能超过200个字符")]
        [MinLength(2, ErrorMessage = "标题长度不能少于2个字符")]
        public string Title { get; set; }

        [MaxLength(1000, ErrorMessage = "描述长度不能超过1000个字符")]
        public string? Description { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        public int? DepartmentId { get; set; }

        public int? AssignedUserId { get; set; }
    }
}
