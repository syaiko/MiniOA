using MiniOA.Core.DTOs;
using MiniOA.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Interfaces
{
    public interface IDepartmentService
    {
        Task<ApiResult<List<DepartmentDto>>> GetDepartmentTreeAsync(int currentUserId, string currentUserRole);
        Task<ApiResult<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto input);
        Task<ApiResult<bool>> UpdateDepartmentAsync(int id, UpdateDepartmentDto input);
        Task<ApiResult<bool>> DeleteDepartmentAsync(int id);
        Task<ApiResult<List<UserDto>>> GetDepartmentUsersAsync(int departmentId);
        Task<ApiResult<List<DepartmentDto>>> GetSubDepartmentsAsync(int parentId);
    }
}
