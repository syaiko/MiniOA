using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniOA.Core.DTOs;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Infrastructure.Services;

namespace MiniOA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 获取用户列表（根据用户角色过滤）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int? departmentId = null, [FromQuery] string? search = null)
        {
            var result = await _userService.GetUsersAsync(departmentId, search, CurrentUserId, CurrentUserRole);
            return Ok(result);
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.SuperAdmin) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto input)
        {
            var result = await _userService.CreateUserAsync(input);
            return Ok(result);
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(UserRole.SuperAdmin) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto input)
        {
            var result = await _userService.UpdateUserAsync(id, input);
            return Ok(result);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(UserRole.SuperAdmin) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = nameof(UserRole.SuperAdmin) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _userService.GetUserAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = nameof(UserRole.SuperAdmin) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto input)
        {
            var result = await _userService.ResetPasswordAsync(id, input.NewPassword);
            return Ok(result);
        }

        // ==================== 个人档案管理（当前用户操作自己的资料） ====================
        
        /// <summary>
        /// 获取当前用户个人资料
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _userService.GetUserAsync(CurrentUserId);
            return Ok(result);
        }

        /// <summary>
        /// 更新当前用户个人资料
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto input)
        {
            var result = await _userService.UpdateProfileAsync(CurrentUserId, input);
            return Ok(result);
        }

        /// <summary>
        /// 修改当前用户密码
        /// </summary>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto input)
        {
            var result = await _userService.ChangePasswordAsync(CurrentUserId, input.CurrentPassword, input.NewPassword);
            return Ok(result);
        }
    }
}
