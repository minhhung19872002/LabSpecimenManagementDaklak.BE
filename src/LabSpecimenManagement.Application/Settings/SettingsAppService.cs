using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace LabSpecimenManagement.Settings;

[Authorize]
public class SettingsAppService : ApplicationService, ISettingsAppService
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly ISettingManager _settingManager;
    private readonly ISettingProvider _settingProvider;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public SettingsAppService(
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        ISettingManager settingManager,
        ISettingProvider settingProvider,
        IRepository<Department, Guid> departmentRepository)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _settingManager = settingManager;
        _settingProvider = settingProvider;
        _departmentRepository = departmentRepository;
    }

    #region User Profile

    public async Task<UserProfileDto> GetUserProfileAsync()
    {
        var userId = CurrentUser.Id ?? throw new UserFriendlyException("User not authenticated");
        var user = await _userRepository.GetAsync(userId);

        var departmentId = user.GetProperty<Guid?>("DepartmentId");
        string? departmentName = null;
        if (departmentId.HasValue)
        {
            var department = await _departmentRepository.FindAsync(departmentId.Value);
            departmentName = department?.Name;
        }

        var fullName = $"{user.Surname} {user.Name}".Trim();

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Department = departmentName,
            DepartmentId = departmentId,
            Position = user.GetProperty<string>("Position")
        };
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileDto input)
    {
        var userId = CurrentUser.Id ?? throw new UserFriendlyException("User not authenticated");
        var user = await _userRepository.GetAsync(userId);

        // Parse fullName
        if (!string.IsNullOrEmpty(input.FullName))
        {
            var nameParts = input.FullName.Trim().Split(' ', 2);
            user.Surname = nameParts.Length > 1 ? nameParts[0] : string.Empty;
            user.Name = nameParts.Length > 1 ? nameParts[1] : nameParts[0];
        }

        if (!string.IsNullOrEmpty(input.Email))
        {
            await _userManager.SetEmailAsync(user, input.Email);
        }

        if (input.PhoneNumber != null)
        {
            user.SetPhoneNumber(input.PhoneNumber, false);
        }

        await _userManager.UpdateAsync(user);

        return await GetUserProfileAsync();
    }

    public async Task ChangePasswordAsync(ChangePasswordDto input)
    {
        var userId = CurrentUser.Id ?? throw new UserFriendlyException("User not authenticated");
        var user = await _userRepository.GetAsync(userId);

        var result = await _userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new UserFriendlyException(errors);
        }
    }

    #endregion

    #region System Settings

    public async Task<SystemSettingsDto> GetSystemSettingsAsync()
    {
        var workingDaysStr = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.WorkingDays);
        var workingDays = string.IsNullOrEmpty(workingDaysStr)
            ? new List<string>()
            : workingDaysStr.Split(',').ToList();

        return new SystemSettingsDto
        {
            LabName = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.LabName) ?? string.Empty,
            LabCode = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.LabCode) ?? string.Empty,
            Address = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.Address) ?? string.Empty,
            DefaultTAT = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.DefaultTAT), out var tat) ? tat : 5,
            WorkingDays = workingDays,
            Language = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.Language) ?? "vi",
            Timezone = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.System.Timezone) ?? "Asia/Ho_Chi_Minh"
        };
    }

    public async Task<SystemSettingsDto> UpdateSystemSettingsAsync(SystemSettingsDto input)
    {
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.LabName, input.LabName);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.LabCode, input.LabCode);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.Address, input.Address);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.DefaultTAT, input.DefaultTAT.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.WorkingDays, string.Join(",", input.WorkingDays));
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.Language, input.Language);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.System.Timezone, input.Timezone);

        return await GetSystemSettingsAsync();
    }

    #endregion

    #region Email Settings

    public async Task<EmailSettingsDto> GetEmailSettingsAsync()
    {
        return new EmailSettingsDto
        {
            SmtpHost = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpHost) ?? string.Empty,
            SmtpPort = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpPort), out var port) ? port : 587,
            SmtpUsername = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpUsername) ?? string.Empty,
            SmtpPassword = null, // Don't return password
            EnableSsl = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.EnableSsl), out var ssl) && ssl,
            FromEmail = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.FromEmail) ?? string.Empty,
            FromName = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.FromName) ?? string.Empty
        };
    }

    public async Task<EmailSettingsDto> UpdateEmailSettingsAsync(UpdateEmailSettingsDto input)
    {
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.SmtpHost, input.SmtpHost);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.SmtpPort, input.SmtpPort.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.SmtpUsername, input.SmtpUsername);

        // Only update password if provided
        if (!string.IsNullOrEmpty(input.SmtpPassword))
        {
            await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.SmtpPassword, input.SmtpPassword);
        }

        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.EnableSsl, input.EnableSsl.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.FromEmail, input.FromEmail);
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Email.FromName, input.FromName);

        return await GetEmailSettingsAsync();
    }

    public async Task TestEmailAsync(TestEmailDto input)
    {
        var smtpHost = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpHost);
        var smtpPortStr = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpPort);
        var smtpUsername = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpUsername);
        var smtpPassword = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.SmtpPassword);
        var enableSslStr = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.EnableSsl);
        var fromEmail = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.FromEmail);
        var fromName = await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Email.FromName);

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(fromEmail))
        {
            throw new UserFriendlyException("Vui lòng cấu hình đầy đủ thông tin SMTP trước khi test.");
        }

        var smtpPort = int.TryParse(smtpPortStr, out var port) ? port : 587;
        var enableSsl = bool.TryParse(enableSslStr, out var ssl) && ssl;

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName ?? "Lab Specimen Management"),
                Subject = "Test Email - Lab Specimen Management",
                Body = @"
                    <h2>Email Test thành công!</h2>
                    <p>Đây là email test từ hệ thống Lab Specimen Management.</p>
                    <p>Nếu bạn nhận được email này, cấu hình SMTP đã hoạt động đúng.</p>
                    <br/>
                    <p><em>Email được gửi tự động, vui lòng không trả lời.</em></p>
                ",
                IsBodyHtml = true
            };
            mailMessage.To.Add(input.ToEmail);

            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException($"Gửi email thất bại: {ex.Message}");
        }
    }

    #endregion

    #region Notification Settings

    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync()
    {
        return new NotificationSettingsDto
        {
            EmailEnabled = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.EmailEnabled), out var email) && email,
            NewRequestAlert = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.NewRequestAlert), out var newReq) && newReq,
            ResultApprovalAlert = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.ResultApprovalAlert), out var approval) && approval,
            TATWarningAlert = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.TATWarningAlert), out var tat) && tat,
            NCRAlert = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.NCRAlert), out var ncr) && ncr,
            WeeklyReport = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.WeeklyReport), out var weekly) && weekly,
            MonthlyReport = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Notification.MonthlyReport), out var monthly) && monthly
        };
    }

    public async Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(NotificationSettingsDto input)
    {
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.EmailEnabled, input.EmailEnabled.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.NewRequestAlert, input.NewRequestAlert.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.ResultApprovalAlert, input.ResultApprovalAlert.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.TATWarningAlert, input.TATWarningAlert.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.NCRAlert, input.NCRAlert.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.WeeklyReport, input.WeeklyReport.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Notification.MonthlyReport, input.MonthlyReport.ToString().ToLower());

        return await GetNotificationSettingsAsync();
    }

    #endregion

    #region Security Settings

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync()
    {
        return new SecuritySettingsDto
        {
            RequireStrongPassword = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.RequireStrongPassword), out var strong) && strong,
            SessionTimeout = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.SessionTimeout), out var timeout) ? timeout : 30,
            MaxLoginAttempts = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.MaxLoginAttempts), out var attempts) ? attempts : 5,
            LockoutDuration = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.LockoutDuration), out var lockout) ? lockout : 15,
            PasswordExpiryDays = int.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.PasswordExpiryDays), out var expiry) ? expiry : 90,
            TwoFactorEnabled = bool.TryParse(await _settingProvider.GetOrNullAsync(LabSpecimenManagementSettings.Security.TwoFactorEnabled), out var tfa) && tfa
        };
    }

    public async Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(SecuritySettingsDto input)
    {
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.RequireStrongPassword, input.RequireStrongPassword.ToString().ToLower());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.SessionTimeout, input.SessionTimeout.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.MaxLoginAttempts, input.MaxLoginAttempts.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.LockoutDuration, input.LockoutDuration.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.PasswordExpiryDays, input.PasswordExpiryDays.ToString());
        await _settingManager.SetGlobalAsync(LabSpecimenManagementSettings.Security.TwoFactorEnabled, input.TwoFactorEnabled.ToString().ToLower());

        return await GetSecuritySettingsAsync();
    }

    #endregion
}
