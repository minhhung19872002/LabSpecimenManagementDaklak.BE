using System.Collections.Generic;

namespace LabSpecimenManagement.Settings;

public class SystemSettingsDto
{
    public string LabName { get; set; } = string.Empty;
    public string LabCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int DefaultTAT { get; set; } = 5;
    public List<string> WorkingDays { get; set; } = new();
    public string Language { get; set; } = "vi";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
}
