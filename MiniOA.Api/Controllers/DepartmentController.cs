using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniOA.Core.DTOs;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Infrastructure.Services;

namespace MiniOA.Api.Controllers
{
    public static class UserRoles
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string SuperAdmin = nameof(UserRole.SuperAdmin);
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentController : BaseController
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        /// <summary>
        /// 获取部门树（根据用户角色过滤）
        /// </summary>
        [HttpGet("tree")]
        public async Task<IActionResult> GetDepartmentTree()
        {
            var result = await _departmentService.GetDepartmentTreeAsync(CurrentUserId, CurrentUserRole);
            return Ok(result);
        }

        /// <summary>
        /// 创建部门
        /// </summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.SuperAdmin)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto input)
        {
            var result = await _departmentService.CreateDepartmentAsync(input);
            return Ok(result);
        }

        /// <summary>
        /// 更新部门
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.SuperAdmin)]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto input)
        {
            var result = await _departmentService.UpdateDepartmentAsync(id, input);
            return Ok(result);
        }

        /// <summary>
        /// 删除部门
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.SuperAdmin)]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var result = await _departmentService.DeleteDepartmentAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// 获取部门用户
        /// </summary>
        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetDepartmentUsers(int id)
        {
            var result = await _departmentService.GetDepartmentUsersAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// 获取子部门
        /// </summary>
        [HttpGet("{parentId}/children")]
        public async Task<IActionResult> GetSubDepartments(int parentId)
        {
            var result = await _departmentService.GetSubDepartmentsAsync(parentId);
            return Ok(result);
        }
    }
}
