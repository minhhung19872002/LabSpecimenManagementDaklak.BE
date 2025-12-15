using Volo.Abp.Settings;

namespace LabSpecimenManagement.Settings;

public class LabSpecimenManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // System Settings
        context.Add(
            new SettingDefinition(
                LabSpecimenManagementSettings.System.LabName,
                "Phòng Thí nghiệm Đắk Lắk",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.LabCode,
                "VILAS-001",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.Address,
                "123 Nguyễn Tất Thành, TP. Buôn Ma Thuột",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.DefaultTAT,
                "5",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.WorkingDays,
                "monday,tuesday,wednesday,thursday,friday",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.Language,
                "vi",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.System.Timezone,
                "Asia/Ho_Chi_Minh",
                isVisibleToClients: true
            )
        );

        // Email/SMTP Settings (not visible to clients for security)
        context.Add(
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.SmtpHost,
                "smtp.gmail.com"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.SmtpPort,
                "587"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.SmtpUsername,
                ""
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.SmtpPassword,
                "",
                isEncrypted: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.EnableSsl,
                "true"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.FromEmail,
                ""
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Email.FromName,
                "Lab Specimen Management"
            )
        );

        // Notification Settings
        context.Add(
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.EmailEnabled,
                "true",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.NewRequestAlert,
                "true",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.ResultApprovalAlert,
                "true",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.TATWarningAlert,
                "true",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.NCRAlert,
                "true",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.WeeklyReport,
                "false",
                isVisibleToClients: true
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Notification.MonthlyReport,
                "true",
                isVisibleToClients: true
            )
        );

        // Security Settings
        context.Add(
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.RequireStrongPassword,
                "true"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.SessionTimeout,
                "30"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.MaxLoginAttempts,
                "5"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.LockoutDuration,
                "15"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.PasswordExpiryDays,
                "90"
            ),
            new SettingDefinition(
                LabSpecimenManagementSettings.Security.TwoFactorEnabled,
                "false"
            )
        );
    }
}
