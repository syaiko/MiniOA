namespace MiniOA.Core.DTOs
{
    /// <summary>
    /// 任务趋势统计DTO
    /// </summary>
    public class TaskTrendDto
    {
        /// <summary>
        /// 趋势数据列表
        /// </summary>
        public List<TaskTrendItemDto> TrendData { get; set; } = new();
    }

    /// <summary>
    /// 任务趋势项DTO
    /// </summary>
    public class TaskTrendItemDto
    {
        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 月份
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 月份标签
        /// </summary>
        public string MonthLabel { get; set; } = string.Empty;

        /// <summary>
        /// 创建任务数
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// 完成任务数
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 逾期任务数
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// 逾期率
        /// </summary>
        public decimal OverdueRate { get; set; }
    }
}
