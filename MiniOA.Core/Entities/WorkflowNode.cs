using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class WorkflowNode
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string WorkflowType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string NodeType { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; }
        
        public int? ApproverId { get; set; }
        
        public int? DepartmentId { get; set; }
        
        public string? ConditionExpression { get; set; }
        
        public string? NodeConfig { get; set; }
        
        public int? NextNodeId { get; set; }
        
        [Column(TypeName = "nvarchar(500)")]
        public string? CcUserIds { get; set; } // 抄送人员ID列表
        
        public bool IsActive { get; set; } = true;
        
        [ForeignKey("ApproverId")]
        public virtual User? Approver { get; set; }
        
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        
        [ForeignKey("NextNodeId")]
        public virtual WorkflowNode? NextNode { get; set; }
        
        public virtual ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    }
}
