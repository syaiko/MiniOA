using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class WorkflowInstance
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string WorkflowType { get; set; } = string.Empty;
        
        public int CreatorId { get; set; }
        
        public int? CurrentNodeId { get; set; }
        
        [Required]
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
        
        public string? BusinessData { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        [ForeignKey("CreatorId")]
        public virtual User Creator { get; set; } = null!;
        
        [ForeignKey("CurrentNodeId")]
        public virtual WorkflowNode? CurrentNode { get; set; }
        
        public virtual ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    }

}
