namespace LabSpecimenManagement.Settings;

public class SecuritySettingsDto
{
    public bool RequireStrongPassword { get; set; } = true;
    public int SessionTimeout { get; set; } = 30;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDuration { get; set; } = 15;
    public int PasswordExpiryDays { get; set; } = 90;
    public bool TwoFactorEnabled { get; set; } = false;
}
