using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Enums
{
    public enum NotificationType
    {
        Workflow = 0,
        Task = 1,
        System = 2,
        Reminder = 3,
        WorkflowCc = 4 // 工作流抄送通知
    }
}
