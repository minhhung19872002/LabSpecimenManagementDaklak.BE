using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Auth;

/// <summary>
/// Authentication and user management service
/// </summary>
public interface IAuthAppService : IApplicationService
{
    /// <summary>
    /// Login with username/email and password
    /// </summary>
    Task<LoginResultDto> LoginAsync(LoginDto input);

    /// <summary>
    /// Logout current user
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Get current logged-in user information
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync();

    /// <summary>
    /// Update current user profile
    /// </summary>
    Task<CurrentUserDto> UpdateProfileAsync(UpdateProfileDto input);

    /// <summary>
    /// Change password for current user
    /// </summary>
    Task<bool> ChangePasswordAsync(ChangePasswordDto input);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<bool> ForgotPasswordAsync(ForgotPasswordDto input);

    /// <summary>
    /// Reset password with token
    /// </summary>
    Task<bool> ResetPasswordAsync(ResetPasswordDto input);

    /// <summary>
    /// Register new user (if allowed)
    /// </summary>
    Task<RegisterResultDto> RegisterAsync(RegisterDto input);

    /// <summary>
    /// Refresh access token
    /// </summary>
    Task<LoginResultDto> RefreshTokenAsync(string refreshToken);
}
