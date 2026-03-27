using MiniOA.Core.DTOs;
using MiniOA.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Interfaces
{
    public interface IUserService
    {
        Task<ApiResult<List<UserDto>>> GetUsersAsync(int? departmentId = null, string? search = null, int currentUserId = 0, string currentUserRole = "");
        Task<ApiResult<UserDto>> GetUserAsync(int id);
        Task<ApiResult<UserDto>> CreateUserAsync(CreateUserDto input);
        Task<ApiResult<bool>> UpdateUserAsync(int id, UpdateUserDto input);
        Task<ApiResult<bool>> DeleteUserAsync(int id);
        Task<ApiResult<bool>> ResetPasswordAsync(int id, string newPassword);
        
        // 个人档案管理（当前用户操作自己的资料）
        Task<ApiResult<UserDto>> UpdateProfileAsync(int userId, UpdateProfileDto input);
        Task<ApiResult<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
