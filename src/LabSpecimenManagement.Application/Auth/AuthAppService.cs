using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace LabSpecimenManagement.Auth;

[RemoteService(IsEnabled = false)]
public class AuthAppService : ApplicationService, IAuthAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IConfiguration _configuration;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthAppService(
        IdentityUserManager userManager,
        IConfiguration configuration,
        IPermissionChecker permissionChecker,
        IRepository<Department, Guid> departmentRepository,
        IIdentityRoleRepository roleRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _configuration = configuration;
        _permissionChecker = permissionChecker;
        _departmentRepository = departmentRepository;
        _roleRepository = roleRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input)
    {
        var user = await _userManager.FindByNameAsync(input.UserNameOrEmail)
                   ?? await _userManager.FindByEmailAsync(input.UserNameOrEmail);

        if (user == null)
        {
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng."
            };
        }

        if (!user.IsActive)
        {
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = "Tài khoản đã bị vô hiệu hóa."
            };
        }

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, input.Password);
        if (!isPasswordValid)
        {
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng."
            };
        }

        // Check lockout
        if (await _userManager.IsLockedOutAsync(user))
        {
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = "Tài khoản đã bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau."
            };
        }

        // Get token from OpenIddict
        var tokenResult = await GetTokenAsync(input.UserNameOrEmail, input.Password);

        if (!tokenResult.Success)
        {
            return tokenResult;
        }

        tokenResult.User = await BuildCurrentUserDtoAsync(user);
        return tokenResult;
    }

    public Task LogoutAsync()
    {
        // Client-side should discard the token
        // Server-side token revocation can be implemented if needed
        return Task.CompletedTask;
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync()
    {
        if (CurrentUser.Id == null)
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập.");
        }

        var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);
        return await BuildCurrentUserDtoAsync(user);
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(UpdateProfileDto input)
    {
        if (CurrentUser.Id == null)
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập.");
        }

        var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            user.Name = input.Name;
        }

        if (!string.IsNullOrWhiteSpace(input.Surname))
        {
            user.Surname = input.Surname;
        }

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(input.Position))
        {
            user.SetProperty("Position", input.Position);
        }

        await _userManager.UpdateAsync(user);

        return await BuildCurrentUserDtoAsync(user);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto input)
    {
        if (CurrentUser.Id == null)
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập.");
        }

        var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

        var result = await _userManager.ChangePasswordAsync(
            user,
            input.CurrentPassword,
            input.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException("password", $"Đổi mật khẩu thất bại: {errors}");
        }

        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto input)
    {
        var user = await _userManager.FindByEmailAsync(input.Email);

        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with reset token
        // For now, just log the token (in production, send email)
        Logger.LogInformation($"Password reset token for {input.Email}: {token}");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto input)
    {
        var user = await _userManager.FindByEmailAsync(input.Email);

        if (user == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("User");
        }

        var result = await _userManager.ResetPasswordAsync(user, input.ResetToken, input.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException("password", $"Đặt lại mật khẩu thất bại: {errors}");
        }

        return true;
    }

    public async Task<RegisterResultDto> RegisterAsync(RegisterDto input)
    {
        var existingUser = await _userManager.FindByNameAsync(input.UserName);
        if (existingUser != null)
        {
            return new RegisterResultDto
            {
                Success = false,
                Message = "Tên đăng nhập đã tồn tại.",
                Errors = new List<string> { "Tên đăng nhập đã tồn tại." }
            };
        }

        existingUser = await _userManager.FindByEmailAsync(input.Email);
        if (existingUser != null)
        {
            return new RegisterResultDto
            {
                Success = false,
                Message = "Email đã được sử dụng.",
                Errors = new List<string> { "Email đã được sử dụng." }
            };
        }

        var user = new Volo.Abp.Identity.IdentityUser(
            GuidGenerator.Create(),
            input.UserName,
            input.Email,
            CurrentTenant.Id);

        user.Name = input.Name;
        user.Surname = input.Surname;

        var result = await _userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            return new RegisterResultDto
            {
                Success = false,
                Message = "Đăng ký thất bại.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);
        }

        return new RegisterResultDto
        {
            Success = true,
            UserId = user.Id,
            Message = "Đăng ký thành công."
        };
    }

    public async Task<LoginResultDto> RefreshTokenAsync(string refreshToken)
    {
        // Get base URL from current request context instead of config (same as login)
        var request = _httpContextAccessor.HttpContext?.Request;
        var authority = request != null
            ? $"{request.Scheme}://{request.Host}"
            : _configuration["AuthServer:Authority"];
        var clientId = _configuration["AuthServer:ClientId"] ?? "LabSpecimenManagement_App";

        // Create HttpClient that bypasses SSL certificate validation for development
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", clientId },
            { "scope", "LabSpecimenManagement offline_access" }
        });

        try
        {
            var response = await client.PostAsync($"{authority}/connect/token", content);

            if (!response.IsSuccessStatusCode)
            {
                return new LoginResultDto
                {
                    Success = false,
                    ErrorMessage = "Không thể làm mới token. Vui lòng đăng nhập lại."
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (tokenResponse == null)
            {
                return new LoginResultDto
                {
                    Success = false,
                    ErrorMessage = "Phản hồi không hợp lệ từ server."
                };
            }

            return new LoginResultDto
            {
                Success = true,
                AccessToken = tokenResponse.GetValueOrDefault("access_token")?.ToString(),
                RefreshToken = tokenResponse.GetValueOrDefault("refresh_token")?.ToString(),
                ExpiresIn = int.TryParse(tokenResponse.GetValueOrDefault("expires_in")?.ToString(), out var exp) ? exp : 3600,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing token");
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = "Lỗi khi làm mới token."
            };
        }
    }

    #region Private Methods

    private async Task<LoginResultDto> GetTokenAsync(string username, string password)
    {
        // Get base URL from current request context instead of config
        var request = _httpContextAccessor.HttpContext?.Request;
        var authority = request != null
            ? $"{request.Scheme}://{request.Host}"
            : _configuration["AuthServer:Authority"];
        var clientId = _configuration["AuthServer:ClientId"] ?? "LabSpecimenManagement_App";

        // Create HttpClient that bypasses SSL certificate validation for development
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", username },
            { "password", password },
            { "client_id", clientId },
            { "scope", "LabSpecimenManagement offline_access" }
        });

        try
        {
            var response = await client.PostAsync($"{authority}/connect/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning($"Token request failed: {errorContent}");

                return new LoginResultDto
                {
                    Success = false,
                    ErrorMessage = "Đăng nhập thất bại. Vui lòng kiểm tra thông tin đăng nhập."
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (tokenResponse == null)
            {
                return new LoginResultDto
                {
                    Success = false,
                    ErrorMessage = "Phản hồi không hợp lệ từ server."
                };
            }

            return new LoginResultDto
            {
                Success = true,
                AccessToken = tokenResponse.GetValueOrDefault("access_token")?.ToString(),
                RefreshToken = tokenResponse.GetValueOrDefault("refresh_token")?.ToString(),
                ExpiresIn = int.TryParse(tokenResponse.GetValueOrDefault("expires_in")?.ToString(), out var exp) ? exp : 3600,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting token from {Authority}", authority);
            return new LoginResultDto
            {
                Success = false,
                ErrorMessage = $"Lỗi kết nối đến server xác thực: {ex.Message}"
            };
        }
    }

    private async Task<CurrentUserDto> BuildCurrentUserDtoAsync(Volo.Abp.Identity.IdentityUser user)
    {
        var dto = new CurrentUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TenantId = user.TenantId,
            Position = user.GetProperty<string>("Position")
        };

        // Get DepartmentId
        var departmentId = user.GetProperty<Guid?>("DepartmentId");
        if (departmentId.HasValue)
        {
            dto.DepartmentId = departmentId;
            var department = await _departmentRepository.FindAsync(departmentId.Value);
            dto.DepartmentName = department?.Name;
        }

        // Get UserRole
        dto.UserRole = user.GetProperty<UserRole>("UserRole");

        // Get roles
        var roles = await _userManager.GetRolesAsync(user);
        dto.Roles = roles.ToList();

        // Get permissions (basic set)
        var permissions = new List<string>();
        foreach (var role in roles)
        {
            permissions.Add($"Role:{role}");
        }
        dto.Permissions = permissions;

        return dto;
    }

    #endregion
}
