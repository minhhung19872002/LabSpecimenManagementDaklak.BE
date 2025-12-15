using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Settings;

public interface ISettingsAppService : IApplicationService
{
    // User Profile
    Task<UserProfileDto> GetUserProfileAsync();
    Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileDto input);
    Task ChangePasswordAsync(ChangePasswordDto input);

    // System Settings
    Task<SystemSettingsDto> GetSystemSettingsAsync();
    Task<SystemSettingsDto> UpdateSystemSettingsAsync(SystemSettingsDto input);

    // Email/SMTP Settings
    Task<EmailSettingsDto> GetEmailSettingsAsync();
    Task<EmailSettingsDto> UpdateEmailSettingsAsync(UpdateEmailSettingsDto input);
    Task TestEmailAsync(TestEmailDto input);

    // Notification Settings
    Task<NotificationSettingsDto> GetNotificationSettingsAsync();
    Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(NotificationSettingsDto input);

    // Security Settings
    Task<SecuritySettingsDto> GetSecuritySettingsAsync();
    Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(SecuritySettingsDto input);
}
