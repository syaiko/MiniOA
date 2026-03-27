namespace MiniOA.Core.DTOs
{
    /// <summary>
    /// 首页仪表盘数据DTO - 支持多角色数据
    /// </summary>
    public class DashboardDto
    {
        // ========== 个人数据（所有角色可见） ==========
        /// <summary>
        /// 我的待办任务数
        /// </summary>
        public int MyPendingTasks { get; set; }

        /// <summary>
        /// 我已完成任务数
        /// </summary>
        public int MyCompletedTasks { get; set; }

        /// <summary>
        /// 我的申请总数
        /// </summary>
        public int MyApplications { get; set; }

        /// <summary>
        /// 我的未读消息数
        /// </summary>
        public int MyUnreadNotifications { get; set; }

        /// <summary>
        /// 我的任务完成率
        /// </summary>
        public decimal MyCompletionRate { get; set; }

        // ========== 部门数据（部门经理及以上可见） ==========
        /// <summary>
        /// 是否显示部门数据
        /// </summary>
        public bool ShowDepartmentData { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// 部门待办总数
        /// </summary>
        public int DepartmentPendingTasks { get; set; }

        /// <summary>
        /// 部门已完成任务数
        /// </summary>
        public int DepartmentCompletedTasks { get; set; }

        /// <summary>
        /// 部门成员数
        /// </summary>
        public int DepartmentMemberCount { get; set; }

        /// <summary>
        /// 部门任务完成率
        /// </summary>
        public decimal DepartmentCompletionRate { get; set; }

        /// <summary>
        /// 部门成员任务完成率列表
        /// </summary>
        public List<MemberTaskDto> DepartmentMembers { get; set; } = new();

        /// <summary>
        /// 部门任务趋势（近6个月）
        /// </summary>
        public List<MonthlyTrendDto> DepartmentTrend { get; set; } = new();

        // ========== 全局数据（管理员/超级管理员可见） ==========
        /// <summary>
        /// 是否显示全局数据
        /// </summary>
        public bool ShowGlobalData { get; set; }

        /// <summary>
        /// 全公司任务总数
        /// </summary>
        public int GlobalTotalTasks { get; set; }

        /// <summary>
        /// 全公司待办任务数
        /// </summary>
        public int GlobalPendingTasks { get; set; }

        /// <summary>
        /// 全公司已完成任务数
        /// </summary>
        public int GlobalCompletedTasks { get; set; }

        /// <summary>
        /// 全公司逾期任务数
        /// </summary>
        public int GlobalOverdueTasks { get; set; }

        /// <summary>
        /// 全公司任务完成率
        /// </summary>
        public decimal GlobalCompletionRate { get; set; }

        /// <summary>
        /// 全公司逾期率
        /// </summary>
        public decimal GlobalOverdueRate { get; set; }

        /// <summary>
        /// 各部门任务统计
        /// </summary>
        public List<DepartmentTaskDto> DepartmentStats { get; set; } = new();

        /// <summary>
        /// 全公司任务趋势（近12个月）
        /// </summary>
        public List<MonthlyTrendDto> GlobalTrend { get; set; } = new();

        // ========== 最近动态（所有角色可见） ==========
        /// <summary>
        /// 最近动态列表
        /// </summary>
        public List<ActivityDto> RecentActivities { get; set; } = new();
    }

    /// <summary>
    /// 成员任务统计DTO
    /// </summary>
    public class MemberTaskDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal CompletionRate { get; set; }
    }

    /// <summary>
    /// 部门任务统计DTO
    /// </summary>
    public class DepartmentTaskDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int MemberCount { get; set; }
        public decimal CompletionRate { get; set; }
    }

    /// <summary>
    /// 月度趋势DTO
    /// </summary>
    public class MonthlyTrendDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int CreatedCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>
    /// 活动动态DTO
    /// </summary>
    public class ActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }
}
