namespace MiniOA.Core.Enums
{
    public enum UserRole
    {
        SuperAdmin = 0,    // 超级管理员
        Admin = 1,         // 管理员
        DepartmentManager = 2,  // 部门经理
        TeamLeader = 3,    // 组长
        Employee = 4,      // 普通员工
        Intern = 5         // 实习生
    }

    public static class UserRoleExtensions
    {
        public static readonly Dictionary<UserRole, string> RoleNames = new()
        {
            { UserRole.SuperAdmin, "超级管理员" },
            { UserRole.Admin, "管理员" },
            { UserRole.DepartmentManager, "部门经理" },
            { UserRole.TeamLeader, "组长" },
            { UserRole.Employee, "普通员工" },
            { UserRole.Intern, "实习生" }
        };

        public static string GetDisplayName(this UserRole role)
        {
            return RoleNames.TryGetValue(role, out var name) ? name : role.ToString();
        }

        public static UserRole FromString(string roleString)
        {
            var role = roleString?.Trim();
            return role switch
            {
                "超级管理员" => UserRole.SuperAdmin,
                "管理员" => UserRole.Admin,
                "部门经理" => UserRole.DepartmentManager,
                "组长" => UserRole.TeamLeader,
                "普通员工" => UserRole.Employee,
                "实习生" => UserRole.Intern,
                _ => UserRole.Employee
            };
        }
    }
}
