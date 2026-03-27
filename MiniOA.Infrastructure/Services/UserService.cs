using Microsoft.EntityFrameworkCore;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Models;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Enums;
using MiniOA.Infrastructure.Utils;

namespace MiniOA.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResult<List<UserDto>>> GetUsersAsync(int? departmentId = null, string? search = null, int currentUserId = 0, string currentUserRole = "")
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.Department);

            // 解析角色
            UserRole role = UserRole.Employee;
            Enum.TryParse(currentUserRole, out role);
            
            // 获取当前用户信息
            var currentUser = await _context.Users.FindAsync(currentUserId);
            var userDeptId = currentUser?.DepartmentId;

            // 根据角色过滤用户
            if ((int)role > 1) // 非管理员及以上
            {
                if (departmentId.HasValue)
                {
                    // 如果指定了部门ID
                    if (role == UserRole.Employee) // 普通员工
                    {
                        // 只能看到自己部门的用户
                        if (departmentId.Value != userDeptId)
                        {
                            return ApiResult<List<UserDto>>.Ok(new List<UserDto>(), "获取用户列表成功");
                        }
                    }
                    else // 组长及以上（2,3）
                    {
                        // 跨部门时无法选择成员
                        if (departmentId.Value != userDeptId)
                        {
                            return ApiResult<List<UserDto>>.Ok(new List<UserDto>(), "获取用户列表成功");
                        }
                    }
                    
                    // 获取该部门及其所有子部门的ID列表
                    var departmentIds = await GetDepartmentAndChildrenIdsAsync(departmentId.Value);
                    query = query.Where(u => departmentIds.Contains(u.DepartmentId.Value));
                }
                else
                {
                    // 没有指定部门时，非管理员返回空列表
                    return ApiResult<List<UserDto>>.Ok(new List<UserDto>(), "获取用户列表成功");
                }
            }
            else // 管理员（0,1）
            {
                if (departmentId.HasValue)
                {
                    // 获取该部门及其所有子部门的ID列表
                    var departmentIds = await GetDepartmentAndChildrenIdsAsync(departmentId.Value);
                    query = query.Where(u => departmentIds.Contains(u.DepartmentId.Value));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(u => 
                    u.Username.Contains(searchTerm) || 
                    u.FullName.Contains(searchTerm));
            }

            var users = await query
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    Email = u.Email,
                    Role = u.Role,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    DepartmentFullPath = u.Department != null ? u.Department.FullPath : null,
                    IsActive = u.IsActive,
                    CreateTime = u.CreateTime
                })
                .OrderBy(u => u.CreateTime)
                .ToListAsync();

            return ApiResult<List<UserDto>>.Ok(users, "获取用户列表成功");
        }

        // 递归获取部门及其所有子部门的ID
        private async Task<List<int>> GetDepartmentAndChildrenIdsAsync(int departmentId)
        {
            var ids = new List<int> { departmentId };
            
            // 获取所有部门
            var allDepartments = await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => new { d.Id, d.ParentId })
                .ToListAsync();

            // 递归查找子部门
            void FindChildren(int parentId)
            {
                var children = allDepartments.Where(d => d.ParentId == parentId).ToList();
                foreach (var child in children)
                {
                    ids.Add(child.Id);
                    FindChildren(child.Id);
                }
            }

            FindChildren(departmentId);
            return ids;
        }

        public async Task<ApiResult<UserDto>> GetUserAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
                return ApiResult<UserDto>.Fail("用户不存在", 404);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                DepartmentFullPath = user.Department?.FullPath,
                IsActive = user.IsActive,
                CreateTime = user.CreateTime
            };

            return ApiResult<UserDto>.Ok(userDto, "获取用户信息成功");
        }

        public async Task<ApiResult<UserDto>> CreateUserAsync(CreateUserDto input)
        {
            // 验证用户名唯一性
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == input.Username);
            if (usernameExists)
                return ApiResult<UserDto>.Fail("用户名已存在", 400);

            // 验证部门存在
            if (input.DepartmentId.HasValue)
            {
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == input.DepartmentId.Value && d.IsActive);
                if (!departmentExists)
                    return ApiResult<UserDto>.Fail("部门不存在", 400);
            }

            // 创建密码哈希
            PasswordHelper.CreatePWHash(input.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                Username = input.Username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FullName = input.FullName,
                Role = input.Role,
                DepartmentId = input.DepartmentId,
                IsActive = true,
                CreateTime = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                IsActive = user.IsActive,
                CreateTime = user.CreateTime
            };

            return ApiResult<UserDto>.Ok(userDto, "创建用户成功");
        }

        public async Task<ApiResult<bool>> UpdateUserAsync(int id, UpdateUserDto input)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return ApiResult<bool>.Fail("用户不存在", 404);

            // 验证部门存在
            if (input.DepartmentId.HasValue)
            {
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == input.DepartmentId.Value && d.IsActive);
                if (!departmentExists)
                    return ApiResult<bool>.Fail("部门不存在", 400);
            }

            user.FullName = input.FullName;
            user.Role = input.Role;

            if(input.Role == UserRole.SuperAdmin && user.Role != UserRole.SuperAdmin)
            {
                return ApiResult<bool>.Fail("权限不足", 403);
            }

            user.DepartmentId = input.DepartmentId;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "更新用户成功");
        }

        public async Task<ApiResult<bool>> DeleteUserAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Tasks)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResult<bool>.Fail("用户不存在", 404);

            if (user.Tasks.Any())
                return ApiResult<bool>.Fail("用户还有任务，无法删除", 400);

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "删除用户成功");
        }

        public async Task<ApiResult<bool>> ResetPasswordAsync(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return ApiResult<bool>.Fail("用户不存在", 404);

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return ApiResult<bool>.Fail("密码长度不能少于6个字符", 400);

            // 创建新密码哈希
            PasswordHelper.CreatePWHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
            
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "重置密码成功");
        }

        // 个人档案管理（当前用户操作自己的资料）
        public async Task<ApiResult<UserDto>> UpdateProfileAsync(int currentUserId, UpdateProfileDto input)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            
            if (user == null)
                return ApiResult<UserDto>.Fail("用户不存在", 404);

            // 更新个人资料字段
            user.FullName = input.FullName;
            user.Phone = input.Phone;
            user.Email = input.Email;

            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                DepartmentFullPath = user.Department?.FullPath,
                IsActive = user.IsActive,
                CreateTime = user.CreateTime
            };

            return ApiResult<UserDto>.Ok(userDto, "更新个人资料成功");
        }

        public async Task<ApiResult<bool>> ChangePasswordAsync(int currentUserId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null)
                return ApiResult<bool>.Fail("用户不存在", 404);

            // 验证当前密码
            if (!PasswordHelper.VerifyPW(currentPassword, user.PasswordHash, user.PasswordSalt))
                return ApiResult<bool>.Fail("当前密码不正确", 400);

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return ApiResult<bool>.Fail("新密码长度不能少于6个字符", 400);

            // 创建新密码哈希
            PasswordHelper.CreatePWHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
            
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "修改密码成功");
        }
    }
}
