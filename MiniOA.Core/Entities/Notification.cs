using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using MiniOA.Core.Enums;
using System.Text.Json.Serialization;

namespace MiniOA.Core.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        public int ReceiverId { get; set; }
        
        [Required]
        public NotificationType Type { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        public int? RelatedId { get; set; }
        
        [MaxLength(50)]
        public string? RelatedType { get; set; }
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
    }

}
