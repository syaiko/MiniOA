using System.ComponentModel.DataAnnotations;
using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class SubmitWorkflowInputDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string WorkflowType { get; set; } = string.Empty;
        
        public string? BusinessData { get; set; }
    }

    public class ApproveInputDto
    {
        [Required]
        public int InstanceId { get; set; }
        
        [Required]
        public ApprovalAction Action { get; set; }
        
        [MaxLength(500)]
        public string? Comment { get; set; }
    }

    public class WorkflowInstanceDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public int? CurrentNodeId { get; set; }
        public string? CurrentNodeName { get; set; }
        public WorkflowStatus Status { get; set; }
        public string? BusinessData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class ApprovalRecordDto
    {
        public int Id { get; set; }
        public int WorkflowInstanceId { get; set; }
        public string? NodeName { get; set; }
        public int ApproverId { get; set; }
        public string? ApproverName { get; set; }
        public ApprovalAction Action { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int? ApproverId { get; set; }
        public string? ApproverName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }
}
