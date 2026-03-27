using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiniOA.Core.DTOs;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Models;
using MiniOA.Infrastructure.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MiniOA.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        public readonly AppDbContext _context;
        public readonly JwtSettings _jwtSettings;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<ApiResult<object>> LoginAsync(LoginDto loginDto)
        {
            var username = loginDto.Username?.Trim();
            var password = loginDto.Password?.Trim();

            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return ApiResult<object>.Fail("该用户不存在！", 401);

            if (!user.IsActive) return ApiResult<object>.Fail("该用户已被禁用,请联系管理员！", 403);

            if (!PasswordHelper.VerifyPW(password, user.PasswordHash, user.PasswordSalt))
            {
                return ApiResult<object>.Fail("用户名或密码错误！", 401);
            };

            var token = GenerateJwtToken(user);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                Token = token
            };
            
            return ApiResult<object>.Ok(userInfo, "登录成功！");
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName",user.FullName ?? "")
            };

            var token = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
                expires: DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
