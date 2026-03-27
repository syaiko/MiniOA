using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Enums
{
    public enum WorkflowStatus
    {
        Pending = 0,
        InProgress = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        Returned = 5
    }
}
