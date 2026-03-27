

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MiniOA.Core.Enums;

namespace MiniOA.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class BaseController : Controller
    {
        //把 Token 里的 Claims（用户 ID、角色等）解构出来

        //直接获取当前登录用户ID
        protected int CurrentUserId => Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? "0" );

        
        //获取当前用户角色
        protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";

        //判断是否为管理员
        protected bool IsAdmin => CurrentUserRole == (UserRole.Admin).ToString() || CurrentUserRole == (UserRole.SuperAdmin).ToString();

        //判断是否为部门经理以上
        protected bool IsDepartmentManagerOrAbove => IsAdmin || CurrentUserRole == (UserRole.DepartmentManager).ToString();

        //判断是否为组长以上
        protected bool IsTeamLeaderOrAbove => IsDepartmentManagerOrAbove || CurrentUserRole == (UserRole.TeamLeader).ToString();
    }
}
