namespace LabSpecimenManagement.Settings;

public static class LabSpecimenManagementSettings
{
    private const string Prefix = "LabSpecimenManagement";

    // System Settings
    public static class System
    {
        private const string SystemPrefix = Prefix + ".System";

        public const string LabName = SystemPrefix + ".LabName";
        public const string LabCode = SystemPrefix + ".LabCode";
        public const string Address = SystemPrefix + ".Address";
        public const string DefaultTAT = SystemPrefix + ".DefaultTAT";
        public const string WorkingDays = SystemPrefix + ".WorkingDays";
        public const string Language = SystemPrefix + ".Language";
        public const string Timezone = SystemPrefix + ".Timezone";
    }

    // Email/SMTP Settings
    public static class Email
    {
        private const string EmailPrefix = Prefix + ".Email";

        public const string SmtpHost = EmailPrefix + ".SmtpHost";
        public const string SmtpPort = EmailPrefix + ".SmtpPort";
        public const string SmtpUsername = EmailPrefix + ".SmtpUsername";
        public const string SmtpPassword = EmailPrefix + ".SmtpPassword";
        public const string EnableSsl = EmailPrefix + ".EnableSsl";
        public const string FromEmail = EmailPrefix + ".FromEmail";
        public const string FromName = EmailPrefix + ".FromName";
    }

    // Notification Settings
    public static class Notification
    {
        private const string NotificationPrefix = Prefix + ".Notification";

        public const string EmailEnabled = NotificationPrefix + ".EmailEnabled";
        public const string NewRequestAlert = NotificationPrefix + ".NewRequestAlert";
        public const string ResultApprovalAlert = NotificationPrefix + ".ResultApprovalAlert";
        public const string TATWarningAlert = NotificationPrefix + ".TATWarningAlert";
        public const string NCRAlert = NotificationPrefix + ".NCRAlert";
        public const string WeeklyReport = NotificationPrefix + ".WeeklyReport";
        public const string MonthlyReport = NotificationPrefix + ".MonthlyReport";
    }

    // Security Settings
    public static class Security
    {
        private const string SecurityPrefix = Prefix + ".Security";

        public const string RequireStrongPassword = SecurityPrefix + ".RequireStrongPassword";
        public const string SessionTimeout = SecurityPrefix + ".SessionTimeout";
        public const string MaxLoginAttempts = SecurityPrefix + ".MaxLoginAttempts";
        public const string LockoutDuration = SecurityPrefix + ".LockoutDuration";
        public const string PasswordExpiryDays = SecurityPrefix + ".PasswordExpiryDays";
        public const string TwoFactorEnabled = SecurityPrefix + ".TwoFactorEnabled";
    }
}
