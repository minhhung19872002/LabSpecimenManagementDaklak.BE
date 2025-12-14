namespace LabSpecimenManagement.Settings;

public class NotificationSettingsDto
{
    public bool EmailEnabled { get; set; } = true;
    public bool NewRequestAlert { get; set; } = true;
    public bool ResultApprovalAlert { get; set; } = true;
    public bool TATWarningAlert { get; set; } = true;
    public bool NCRAlert { get; set; } = true;
    public bool WeeklyReport { get; set; } = false;
    public bool MonthlyReport { get; set; } = true;
}
