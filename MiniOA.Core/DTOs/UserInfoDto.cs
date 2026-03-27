using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniOA.Core.Enums;

namespace MiniOA.Core.DTOs
{
    public class UserInfoDto
    {
        public int Id { get; set; } 
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public UserRole Role { get; set; }
        public string? RoleDisplayName => Role.GetDisplayName();
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public bool IsAdmin => Role <= UserRole.Admin;
        public bool IsDepartmentManager => Role <= UserRole.DepartmentManager;
        public bool IsTeamLeader => Role <= UserRole.TeamLeader;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public UserRole Role { get; set; }
        public string? RoleDisplayName => Role.GetDisplayName();
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentFullPath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "邮箱格式不正确")]
        [MaxLength(100)]
        public string? Email { get; set; }

        public UserRole Role { get; set; } = UserRole.Employee;

        public int? DepartmentId { get; set; }
    }

    public class UpdateUserDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        public UserRole Role { get; set; }

        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "邮箱格式不正确")]
        [MaxLength(100)]
        public string? Email { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "新密码长度不能少于6个字符")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
