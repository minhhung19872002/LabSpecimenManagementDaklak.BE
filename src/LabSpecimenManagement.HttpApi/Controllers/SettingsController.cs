using System.Threading.Tasks;
using LabSpecimenManagement.Settings;
using Microsoft.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

[Route("api/v1/[controller]")]
public class SettingsController : LabSpecimenManagementController
{
    private readonly ISettingsAppService _settingsAppService;

    public SettingsController(ISettingsAppService settingsAppService)
    {
        _settingsAppService = settingsAppService;
    }

    #region User Profile

    [HttpGet("profile")]
    public async Task<UserProfileDto> GetUserProfileAsync()
    {
        return await _settingsAppService.GetUserProfileAsync();
    }

    [HttpPut("profile")]
    public async Task<UserProfileDto> UpdateUserProfileAsync([FromBody] UpdateUserProfileDto input)
    {
        return await _settingsAppService.UpdateUserProfileAsync(input);
    }

    [HttpPost("profile/change-password")]
    public async Task ChangePasswordAsync([FromBody] ChangePasswordDto input)
    {
        await _settingsAppService.ChangePasswordAsync(input);
    }

    #endregion

    #region System Settings

    [HttpGet("system")]
    public async Task<SystemSettingsDto> GetSystemSettingsAsync()
    {
        return await _settingsAppService.GetSystemSettingsAsync();
    }

    [HttpPut("system")]
    public async Task<SystemSettingsDto> UpdateSystemSettingsAsync([FromBody] SystemSettingsDto input)
    {
        return await _settingsAppService.UpdateSystemSettingsAsync(input);
    }

    #endregion

    #region Email Settings

    [HttpGet("email")]
    public async Task<EmailSettingsDto> GetEmailSettingsAsync()
    {
        return await _settingsAppService.GetEmailSettingsAsync();
    }

    [HttpPut("email")]
    public async Task<EmailSettingsDto> UpdateEmailSettingsAsync([FromBody] UpdateEmailSettingsDto input)
    {
        return await _settingsAppService.UpdateEmailSettingsAsync(input);
    }

    [HttpPost("email/test")]
    public async Task TestEmailAsync([FromBody] TestEmailDto input)
    {
        await _settingsAppService.TestEmailAsync(input);
    }

    #endregion

    #region Notification Settings

    [HttpGet("notifications")]
    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync()
    {
        return await _settingsAppService.GetNotificationSettingsAsync();
    }

    [HttpPut("notifications")]
    public async Task<NotificationSettingsDto> UpdateNotificationSettingsAsync([FromBody] NotificationSettingsDto input)
    {
        return await _settingsAppService.UpdateNotificationSettingsAsync(input);
    }

    #endregion

    #region Security Settings

    [HttpGet("security")]
    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync()
    {
        return await _settingsAppService.GetSecuritySettingsAsync();
    }

    [HttpPut("security")]
    public async Task<SecuritySettingsDto> UpdateSecuritySettingsAsync([FromBody] SecuritySettingsDto input)
    {
        return await _settingsAppService.UpdateSecuritySettingsAsync(input);
    }

    #endregion
}
