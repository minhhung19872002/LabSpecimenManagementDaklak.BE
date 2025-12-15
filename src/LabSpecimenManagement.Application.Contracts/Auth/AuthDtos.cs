using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Users;

namespace LabSpecimenManagement.Auth;

#region Login DTOs

/// <summary>
/// Login request DTO
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResultDto
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public string? ErrorMessage { get; set; }
    public CurrentUserDto? User { get; set; }
}

#endregion

#region User DTOs

/// <summary>
/// Current logged-in user information
/// </summary>
public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string FullName => $"{Name} {Surname}".Trim();
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public Guid? TenantId { get; set; }

    // Extended properties
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public UserRole UserRole { get; set; }

    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Update current user profile
/// </summary>
public class UpdateProfileDto
{
    [MaxLength(64)]
    public string? Name { get; set; }

    [MaxLength(64)]
    public string? Surname { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(16)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Position { get; set; }
}

#endregion

#region Password DTOs

/// <summary>
/// Change password request
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Forgot password request
/// </summary>
public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Reset password request
/// </summary>
public class ResetPasswordDto
{
    [Required(ErrorMessage = "Token là bắt buộc")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

#endregion

#region Register DTOs

/// <summary>
/// User registration request
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Name { get; set; }

    [MaxLength(64)]
    public string? Surname { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(16)]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Registration result
/// </summary>
public class RegisterResultDto
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
