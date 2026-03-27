using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Enums
{
    public enum myTaskStatus
    {
        Todo = 0,        //待处理
        Processing = 1,  //进行中
        Review = 2,      //审核中
        Completed = 3,   //已完成
        Rejected = 4,    //已驳回
        Overdue = 5      //已逾期
    }
}
