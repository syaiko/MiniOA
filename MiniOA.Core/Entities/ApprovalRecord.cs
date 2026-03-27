using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class ApprovalRecord
    {
        [Key]
        public int Id { get; set; }
        
        public int WorkflowInstanceId { get; set; }
        
        public int NodeId { get; set; }
        
        public int ApproverId { get; set; }
        
        [Required]
        public ApprovalAction Action { get; set; }
        
        [MaxLength(500)]
        public string? Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("WorkflowInstanceId")]
        public virtual WorkflowInstance WorkflowInstance { get; set; } = null!;
        
        [ForeignKey("NodeId")]
        public virtual WorkflowNode Node { get; set; } = null!;
        
        [ForeignKey("ApproverId")]
        public virtual User Approver { get; set; } = null!;
    }

}
