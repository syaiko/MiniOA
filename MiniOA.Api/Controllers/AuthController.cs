using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniOA.Core.DTOs;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Infrastructure;
using MiniOA.Infrastructure.Utils;

namespace MiniOA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ApiResult<object>> Login([FromBody] LoginDto login)
        {
            try
            {
                return await _authService.LoginAsync(login);
            }
            catch (Exception ex)
            {
                return ApiResult<object>.Fail(ex.Message, 500);
            }
        }
    }
}
