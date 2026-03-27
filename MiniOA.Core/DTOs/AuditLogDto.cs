using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        
        public int TaskId { get; set; }
        
        public int UserId { get; set; }
        
        public string Username { get; set; } = string.Empty;
        
        public string? FullName { get; set; }
        
        public myTaskStatus FromStatus { get; set; }
        
        public myTaskStatus ToStatus { get; set; }
        
        public string FromStatusDisplay { get; set; } = string.Empty;
        
        public string ToStatusDisplay { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public string OperationType { get; set; } = string.Empty;
        
        public string? Remarks { get; set; }
    }
}
