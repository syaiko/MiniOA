using Microsoft.EntityFrameworkCore;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Core.Enums;

namespace MiniOA.Infrastructure.Services
{public class DepartmentService : IDepartmentService
    {
        private readonly AppDbContext _context;

        public DepartmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResult<List<DepartmentDto>>> GetDepartmentTreeAsync(int currentUserId, string currentUserRole)
        {
            var departments = await _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Parent)
                .Where(d => d.IsActive)
                .ToListAsync();

            // 获取当前用户信息
            var currentUser = await _context.Users.FindAsync(currentUserId);
            var userDeptId = currentUser?.DepartmentId;
            
            // 解析角色
            UserRole role = UserRole.Employee;
            Enum.TryParse(currentUserRole, out role);

            // 根据角色过滤部门
            if ((int)role > 1) // 非管理员及以上
            {
                if (role == UserRole.Employee) // 普通员工
                {
                    // 只能看到自己部门
                    departments = departments.Where(d => d.Id == userDeptId || 
                        (d.Children != null && d.Children.Any(c => c.Id == userDeptId))).ToList();
                }
                // 组长及以上（2,3）可以看到所有部门
            }

            var departmentDtos = departments.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ParentId = d.ParentId,
                ManagerId = d.ManagerId,
                ManagerName = d.Manager?.FullName,
                Level = d.Level,
                FullPath = d.FullPath,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();

            // 构建树结构
            var tree = BuildTree(departmentDtos);
            return ApiResult<List<DepartmentDto>>.Ok(tree, "获取部门树成功");
        }

        public async Task<ApiResult<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto input)
        {
            // 验证父部门存在
            if (input.ParentId.HasValue)
            {
                var parentExists = await _context.Departments.AnyAsync(d => d.Id == input.ParentId.Value && d.IsActive);
                if (!parentExists)
                    return ApiResult<DepartmentDto>.Fail("父部门不存在", 400);
            }

            // 验证部门负责人存在
            var managerExists = await _context.Users.AnyAsync(u => u.Id == input.ManagerId && u.IsActive);
            if (!managerExists)
                return ApiResult<DepartmentDto>.Fail("部门负责人不存在", 400);

            // 验证部门名称唯一性（同级内）
            var nameExists = await _context.Departments
                .AnyAsync(d => d.Name == input.Name && d.ParentId == input.ParentId && d.IsActive);
            if (nameExists)
                return ApiResult<DepartmentDto>.Fail("同级部门名称已存在", 400);

            var department = new Department
            {
                Name = input.Name,
                Description = input.Description,
                ParentId = input.ParentId,
                ManagerId = input.ManagerId,
                IsActive = true
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var resultDto = new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                ParentId = department.ParentId,
                ManagerId = department.ManagerId,
                Level = department.Level,
                FullPath = department.FullPath,
                IsActive = department.IsActive,
                CreatedAt = department.CreatedAt
            };

            return ApiResult<DepartmentDto>.Ok(resultDto, "创建部门成功");
        }

        public async Task<ApiResult<bool>> UpdateDepartmentAsync(int id, UpdateDepartmentDto input)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return ApiResult<bool>.Fail("部门不存在", 404);

            if (!department.IsActive)
                return ApiResult<bool>.Fail("部门已停用", 400);

            // 验证父部门存在且不是自己
            if (input.ParentId.HasValue)
            {
                if (input.ParentId.Value == id)
                    return ApiResult<bool>.Fail("不能设置自己为父部门", 400);

                var parentExists = await _context.Departments.AnyAsync(d => d.Id == input.ParentId.Value && d.IsActive);
                if (!parentExists)
                    return ApiResult<bool>.Fail("父部门不存在", 400);
            }

            // 验证部门负责人存在
            var managerExists = await _context.Users.AnyAsync(u => u.Id == input.ManagerId && u.IsActive);
            if (!managerExists)
                return ApiResult<bool>.Fail("部门负责人不存在", 400);

            // 验证部门名称唯一性（同级内，排除自己）
            var nameExists = await _context.Departments
                .AnyAsync(d => d.Name == input.Name && d.ParentId == input.ParentId && d.Id != id && d.IsActive);
            if (nameExists)
                return ApiResult<bool>.Fail("同级部门名称已存在", 400);

            department.Name = input.Name;
            department.Description = input.Description;
            department.ParentId = input.ParentId;
            department.ManagerId = input.ManagerId;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "更新部门成功");
        }

        public async Task<ApiResult<bool>> DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Children)
                .Include(d => d.Users)
                .Include(d => d.Tasks)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return ApiResult<bool>.Fail("部门不存在", 404);

            if (department.Children.Any(c => c.IsActive))
                return ApiResult<bool>.Fail("存在子部门，无法删除", 400);

            if (department.Users.Any(u => u.IsActive))
                return ApiResult<bool>.Fail("部门下还有员工，无法删除", 400);

            if (department.Tasks.Any())
                return ApiResult<bool>.Fail("部门下还有任务，无法删除", 400);

            department.IsActive = false;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "删除部门成功");
        }

        public async Task<ApiResult<List<UserDto>>> GetDepartmentUsersAsync(int id)
        {
            var users = await _context.Users
                .Include(u => u.Department)
                .Where(u => u.DepartmentId == id && u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    DepartmentName = u.Department.Name,
                    IsActive = u.IsActive,
                    CreateTime = u.CreateTime
                })
                .ToListAsync();

            return ApiResult<List<UserDto>>.Ok(users, "获取部门用户成功");
        }

        public async Task<ApiResult<List<DepartmentDto>>> GetSubDepartmentsAsync(int parentId)
        {
            var subDepartments = await _context.Departments
                .Include(d => d.Manager)
                .Where(d => d.ParentId == parentId && d.IsActive)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ParentId = d.ParentId,
                    ManagerId = d.ManagerId,
                    ManagerName = d.Manager.FullName,
                    Level = d.Level,
                    FullPath = d.FullPath,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync();

            return ApiResult<List<DepartmentDto>>.Ok(subDepartments, "获取子部门成功");
        }

        private List<DepartmentDto> BuildTree(List<DepartmentDto> departments)
        {
            var rootDepartments = departments.Where(d => d.ParentId == null).ToList();
            
            foreach (var root in rootDepartments)
            {
                root.Children = BuildChildren(root.Id, departments);
            }

            return rootDepartments;
        }

        private List<DepartmentDto> BuildChildren(int parentId, List<DepartmentDto> allDepartments)
        {
            var children = allDepartments.Where(d => d.ParentId == parentId).ToList();
            
            foreach (var child in children)
            {
                child.Children = BuildChildren(child.Id, allDepartments);
            }

            return children;
        }
    }
}
