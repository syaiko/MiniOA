namespace MiniOA.Core.DTOs
{
    /// <summary>
    /// 任务统计报表DTO
    /// </summary>
    public class TaskStatisticsDto
    {
        /// <summary>
        /// 本月完成任务数
        /// </summary>
        public int MonthlyCompletedCount { get; set; }

        /// <summary>
        /// 本月总任务数
        /// </summary>
        public int MonthlyTotalCount { get; set; }

        /// <summary>
        /// 本月完成率
        /// </summary>
        public decimal MonthlyCompletionRate { get; set; }

        /// <summary>
        /// 当前逾期任务数
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// 逾期率
        /// </summary>
        public decimal OverdueRate { get; set; }

        /// <summary>
        /// 各部门任务饱和度
        /// </summary>
        public List<DepartmentTaskSaturationDto> DepartmentSaturation { get; set; } = new();
    }

    /// <summary>
    /// 部门任务饱和度DTO
    /// </summary>
    public class DepartmentTaskSaturationDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        
        /// <summary>
        /// 部门总人数
        /// </summary>
        public int TotalMembers { get; set; }

        /// <summary>
        /// 本月部门任务总数
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 进行中任务数
        /// </summary>
        public int InProgressCount { get; set; }

        /// <summary>
        /// 逾期任务数
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// 任务饱和度（人均任务数）
        /// </summary>
        public decimal SaturationRate { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public decimal CompletionRate { get; set; }
    }
}
