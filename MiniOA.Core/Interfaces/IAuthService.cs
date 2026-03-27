using MiniOA.Core.DTOs;
using MiniOA.Core.Models;

namespace MiniOA.Core.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResult<object>> LoginAsync(LoginDto loginDto);
    }
}
