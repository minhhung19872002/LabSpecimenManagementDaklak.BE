using System.Threading.Tasks;
using LabSpecimenManagement.Auth;
using LabSpecimenManagement.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/auth")]
public class AuthController : LabSpecimenManagementController
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public virtual async Task<LoginResultDto> LoginAsync([FromBody] LoginDto input)
    {
        return await _authAppService.LoginAsync(input);
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public virtual async Task LogoutAsync()
    {
        await _authAppService.LogoutAsync();
    }

    /// <summary>
    /// Get current logged-in user information
    /// </summary>
    [HttpGet("current-user")]
    [Authorize]
    public virtual async Task<CurrentUserDto> GetCurrentUserAsync()
    {
        return await _authAppService.GetCurrentUserAsync();
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public virtual async Task<CurrentUserDto> UpdateProfileAsync([FromBody] UpdateProfileDto input)
    {
        return await _authAppService.UpdateProfileAsync(input);
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public virtual async Task<bool> ChangePasswordAsync([FromBody] ChangePasswordDto input)
    {
        return await _authAppService.ChangePasswordAsync(input);
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public virtual async Task<bool> ForgotPasswordAsync([FromBody] ForgotPasswordDto input)
    {
        return await _authAppService.ForgotPasswordAsync(input);
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public virtual async Task<bool> ResetPasswordAsync([FromBody] ResetPasswordDto input)
    {
        return await _authAppService.ResetPasswordAsync(input);
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public virtual async Task<RegisterResultDto> RegisterAsync([FromBody] RegisterDto input)
    {
        return await _authAppService.RegisterAsync(input);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public virtual async Task<LoginResultDto> RefreshTokenAsync([FromBody] RefreshTokenDto request)
    {
        return await _authAppService.RefreshTokenAsync(request.RefreshToken);
    }
}
